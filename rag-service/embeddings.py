"""
Embedding client abstraction.

All production code depends on ``IEmbeddingClient`` (a Protocol).
Unit tests inject ``FakeEmbeddingClient`` — no real API calls.
The OpenAI implementation is registered only when ``embedding_provider == 'openai'``.
"""

import abc
import hashlib
from typing import Protocol, runtime_checkable


@runtime_checkable
class IEmbeddingClient(Protocol):
    """Contract for generating dense vector embeddings from text."""

    async def embed(self, texts: list[str]) -> list[list[float]]:
        """
        Returns one embedding vector per input text.

        Args:
            texts: A batch of text strings to embed.

        Returns:
            A list of float vectors, one per input, all with the same dimension.
        """
        ...


class OpenAIEmbeddingClient:
    """
    Calls the OpenAI Embeddings API (text-embedding-3-small, 1536 dimensions).
    Uses ``httpx`` directly to avoid the openai SDK dependency for now.
    """

    _API_URL = "https://api.openai.com/v1/embeddings"

    def __init__(self, api_key: str, model: str = "text-embedding-3-small") -> None:
        self._api_key = api_key
        self._model = model

    async def embed(self, texts: list[str]) -> list[list[float]]:
        import httpx

        headers = {
            "Authorization": f"Bearer {self._api_key}",
            "Content-Type": "application/json",
        }
        payload = {"model": self._model, "input": texts}

        async with httpx.AsyncClient(timeout=30) as client:
            response = await client.post(self._API_URL, json=payload, headers=headers)
            response.raise_for_status()
            data = response.json()

        return [item["embedding"] for item in data["data"]]


class FakeEmbeddingClient:
    """
    Deterministic fake for unit tests — never calls any external service.
    Produces a reproducible 1536-dim float vector derived from the text hash.
    """

    DIMENSIONS = 1536

    async def embed(self, texts: list[str]) -> list[list[float]]:
        results: list[list[float]] = []
        for text in texts:
            seed = int(hashlib.md5(text.encode()).hexdigest(), 16)
            # Produce a normalised pseudo-random vector seeded by the text hash
            import random
            rng = random.Random(seed)
            vec = [rng.gauss(0, 1) for _ in range(self.DIMENSIONS)]
            norm = sum(x * x for x in vec) ** 0.5
            results.append([x / norm for x in vec])
        return results


def get_embedding_client(provider: str, api_key: str = "", model: str = "") -> IEmbeddingClient:
    """
    Factory — returns the appropriate embedding client for *provider*.

    Args:
        provider: ``'openai'`` or ``'fake'``.
        api_key:  Required when provider is ``'openai'``.
        model:    Optional model override for the OpenAI client.
    """
    if provider == "openai":
        if not api_key:
            raise ValueError("api_key is required for the OpenAI embedding client")
        return OpenAIEmbeddingClient(api_key=api_key, model=model or "text-embedding-3-small")
    if provider == "fake":
        return FakeEmbeddingClient()
    raise ValueError(f"Unknown embedding provider: {provider!r}")
