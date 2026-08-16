"""
Embedding provider abstraction for NeverMissLead.

Design (per AGENTS.md Multi-Provider AI Strategy):
    - ``EmbeddingProvider`` is an ABC with a single ``embed()`` method.
    - ``LocalEmbeddingProvider`` is the **default** — sentence-transformers
      ``all-MiniLM-L6-v2``, CPU-only, no API key, no cost, 384 dimensions.
      This is what ``docker compose up`` uses out of the box and what CI
      and all unit tests run against.
    - ``OpenAIEmbeddingProvider`` and ``GeminiEmbeddingProvider`` are thin
      wrappers around the official SDKs. They are only instantiated when
      their respective API key is present in config. The SDK *packages* are
      MIT/Apache-licensed and can sit in requirements.txt; what costs money
      is calling the hosted API at runtime.
    - ``FakeEmbeddingClient`` is a deterministic, zero-dependency fake used
      exclusively in unit tests. It matches the ABC so it passes isinstance
      checks as well as the Protocol.

Embedding dimension note (AGENTS.md §Multi-Provider AI Strategy):
    - LocalEmbeddingProvider  → 384  (all-MiniLM-L6-v2)
    - OpenAIEmbeddingProvider → 1536 (text-embedding-3-small, default)
    - GeminiEmbeddingProvider → 768  (text-embedding-004, default)
    - FakeEmbeddingClient     → 384  (matches local dev default)

    ⚠️  Switching providers after documents have been embedded requires
    re-embedding all existing kb_chunks rows — this is expected behaviour,
    not a bug. See AGENTS.md for the trade-off discussion.

Factory:
    ``get_embedding_provider()`` reads the ``EMBEDDING_PROVIDER`` env var
    (``local`` | ``openai`` | ``gemini``), defaults to ``"local"``.
"""

import abc
import hashlib
import os
from typing import Protocol, runtime_checkable


# ── Abstract base class (the canonical contract) ──────────────────────────────


class EmbeddingProvider(abc.ABC):
    """
    Abstract base class for all embedding providers.

    All production code should depend on this type rather than a concrete class.
    Unit tests may inject ``FakeEmbeddingClient`` which also satisfies this
    interface via the ``IEmbeddingClient`` Protocol check.
    """

    @abc.abstractmethod
    async def embed(self, texts: list[str]) -> list[list[float]]:
        """
        Returns one embedding vector per input text.

        Args:
            texts: A non-empty batch of text strings to embed.

        Returns:
            A list of float vectors, one per input, all with the same dimension.
            Vectors are L2-normalised (unit length).
        """


# ── Protocol (for structural typing / backward compat) ───────────────────────


@runtime_checkable
class IEmbeddingClient(Protocol):
    """
    Structural protocol — kept for backward compatibility with existing code
    that type-checks against ``IEmbeddingClient`` rather than ``EmbeddingProvider``.
    Any object with an ``async embed(texts) -> list[list[float]]`` method satisfies
    both this Protocol and ``EmbeddingProvider``.
    """

    async def embed(self, texts: list[str]) -> list[list[float]]:
        ...


# ── Local provider (default — free, offline, no key required) ─────────────────


class LocalEmbeddingProvider(EmbeddingProvider):
    """
    CPU-based embeddings via sentence-transformers ``all-MiniLM-L6-v2``.

    Produces **384-dimensional** unit-normalised vectors.
    No API key, no network call, no cost, no rate limit.
    This is the default provider for local dev, CI, and Docker Compose.

    The model is downloaded to the HuggingFace cache on first use and
    cached locally thereafter (~90 MB).
    """

    DIMENSIONS = 384
    DEFAULT_MODEL = "all-MiniLM-L6-v2"

    def __init__(self, model_name: str = DEFAULT_MODEL) -> None:
        # Lazy import so the package is only required when this provider is used.
        from sentence_transformers import SentenceTransformer  # type: ignore[import]

        self._model = SentenceTransformer(model_name, device="cpu")

    async def embed(self, texts: list[str]) -> list[list[float]]:
        # SentenceTransformer.encode is synchronous but fast on CPU for small batches.
        # For production throughput, wrap in asyncio.to_thread if needed (Week 3+).
        embeddings = self._model.encode(texts, normalize_embeddings=True)
        return [vec.tolist() for vec in embeddings]


# ── OpenAI provider (optional — requires OPENAI_API_KEY) ──────────────────────


class OpenAIEmbeddingProvider(EmbeddingProvider):
    """
    Embeddings via the OpenAI Embeddings API.

    Default model: ``text-embedding-3-small`` → **1536 dimensions**.

    Only instantiated when ``EMBEDDING_PROVIDER=openai`` and
    ``OPENAI_API_KEY`` is set. The ``openai`` package is MIT-licensed
    and safe to include as a dependency; the API call itself is metered.

    ⚠️  If you switch to this provider after indexing documents with
    ``LocalEmbeddingProvider``, all existing kb_chunks rows must be
    re-embedded (different dimension — 1536 vs 384).
    """

    DIMENSIONS = 1536
    DEFAULT_MODEL = "text-embedding-3-small"

    def __init__(self, api_key: str, model: str = DEFAULT_MODEL) -> None:
        # Lazy import — only import openai if this provider is actually used.
        import openai  # type: ignore[import]

        self._client = openai.AsyncOpenAI(api_key=api_key)
        self._model = model

    async def embed(self, texts: list[str]) -> list[list[float]]:
        response = await self._client.embeddings.create(
            model=self._model,
            input=texts,
        )
        return [item.embedding for item in response.data]


# ── Gemini provider (optional — requires GEMINI_API_KEY) ─────────────────────


class GeminiEmbeddingProvider(EmbeddingProvider):
    """
    Embeddings via the Google Generative AI Embeddings API.

    Default model: ``text-embedding-004`` → **768 dimensions**.

    Only instantiated when ``EMBEDDING_PROVIDER=gemini`` and
    ``GEMINI_API_KEY`` is set. The ``google-generativeai`` package is
    Apache-2.0-licensed and safe to include as a dependency.

    ⚠️  If you switch to this provider after indexing documents with
    ``LocalEmbeddingProvider``, all existing kb_chunks rows must be
    re-embedded (different dimension — 768 vs 384).
    """

    DIMENSIONS = 768
    DEFAULT_MODEL = "text-embedding-004"

    def __init__(self, api_key: str, model: str = DEFAULT_MODEL) -> None:
        # Lazy import — only import google.generativeai if this provider is used.
        import google.generativeai as genai  # type: ignore[import]

        genai.configure(api_key=api_key)
        self._genai = genai
        self._model = model

    async def embed(self, texts: list[str]) -> list[list[float]]:
        import asyncio

        result = await asyncio.to_thread(
            self._genai.embed_content,
            model=self._model,
            content=texts,
            task_type="retrieval_document",
        )
        # google-generativeai returns {"embedding": [...]} for single text,
        # or a list of embeddings for batches via embed_content with list input.
        embeddings = result.get("embedding", [])
        if embeddings and isinstance(embeddings[0], float):
            # Single-text response — wrap in a list
            embeddings = [embeddings]
        return embeddings


# ── Fake (deterministic, zero-dependency — unit tests only) ──────────────────


class FakeEmbeddingClient(EmbeddingProvider):
    """
    Deterministic fake for unit tests — **never** calls any external service.

    Produces a reproducible **384-dimensional** unit-normalised float vector
    derived from the MD5 hash of the input text.  Dimension matches
    ``LocalEmbeddingProvider`` so test assertions remain valid when the
    default provider is switched from the old OpenAI-backed fake to local.

    This class satisfies both ``EmbeddingProvider`` (ABC) and
    ``IEmbeddingClient`` (Protocol).
    """

    DIMENSIONS = 384  # Matches LocalEmbeddingProvider — default local dev dimension.

    async def embed(self, texts: list[str]) -> list[list[float]]:
        import random

        results: list[list[float]] = []
        for text in texts:
            seed = int(hashlib.md5(text.encode()).hexdigest(), 16)
            rng = random.Random(seed)
            vec = [rng.gauss(0, 1) for _ in range(self.DIMENSIONS)]
            norm = sum(x * x for x in vec) ** 0.5
            results.append([x / norm for x in vec])
        return results


# ── Factory ───────────────────────────────────────────────────────────────────


def get_embedding_provider() -> EmbeddingProvider:
    """
    Factory that reads ``EMBEDDING_PROVIDER`` from the environment and returns
    the appropriate ``EmbeddingProvider`` instance.

    Valid values (case-insensitive):
        ``local``   — ``LocalEmbeddingProvider`` (default; free, offline, 384 dims)
        ``openai``  — ``OpenAIEmbeddingProvider`` (requires ``OPENAI_API_KEY``)
        ``gemini``  — ``GeminiEmbeddingProvider`` (requires ``GEMINI_API_KEY``)
        ``fake``    — ``FakeEmbeddingClient``     (tests only)

    Raises:
        ValueError: If the provider name is unrecognised.
        ValueError: If a paid provider is selected but its API key is missing.
    """
    provider = os.environ.get("EMBEDDING_PROVIDER", "local").strip().lower()

    if provider == "local":
        return LocalEmbeddingProvider()

    if provider == "openai":
        api_key = os.environ.get("OPENAI_API_KEY", "")
        if not api_key:
            raise ValueError(
                "EMBEDDING_PROVIDER=openai requires OPENAI_API_KEY to be set."
            )
        model = os.environ.get("OPENAI_EMBEDDING_MODEL", OpenAIEmbeddingProvider.DEFAULT_MODEL)
        return OpenAIEmbeddingProvider(api_key=api_key, model=model)

    if provider == "gemini":
        api_key = os.environ.get("GEMINI_API_KEY", "")
        if not api_key:
            raise ValueError(
                "EMBEDDING_PROVIDER=gemini requires GEMINI_API_KEY to be set."
            )
        model = os.environ.get("GEMINI_EMBEDDING_MODEL", GeminiEmbeddingProvider.DEFAULT_MODEL)
        return GeminiEmbeddingProvider(api_key=api_key, model=model)

    if provider == "fake":
        return FakeEmbeddingClient()

    raise ValueError(
        f"Unknown EMBEDDING_PROVIDER: {provider!r}. "
        f"Valid values: 'local', 'openai', 'gemini', 'fake'."
    )
