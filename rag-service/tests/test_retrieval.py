"""
Unit tests for embeddings.py and retrieval.py.

All tests use FakeEmbeddingClient (deterministic, zero-dependency).
No test requires an API key or a real embedding provider to pass.
The factory test uses monkeypatching of os.environ only.
"""

import os
import uuid
from unittest.mock import MagicMock, patch

import pytest

from embeddings import (
    EmbeddingProvider,
    FakeEmbeddingClient,
    GeminiEmbeddingProvider,
    LocalEmbeddingProvider,
    OpenAIEmbeddingProvider,
    get_embedding_provider,
)


# ── FakeEmbeddingClient tests ─────────────────────────────────────────────────


class TestFakeEmbeddingClient:
    """Tests for the deterministic fake used in all other unit tests."""

    @pytest.mark.asyncio
    async def test_returns_correct_dimension(self):
        client = FakeEmbeddingClient()
        result = await client.embed(["hello world"])
        assert len(result) == 1
        # FakeEmbeddingClient.DIMENSIONS matches LocalEmbeddingProvider (384)
        assert len(result[0]) == FakeEmbeddingClient.DIMENSIONS
        assert FakeEmbeddingClient.DIMENSIONS == 384

    @pytest.mark.asyncio
    async def test_same_text_produces_same_vector(self):
        client = FakeEmbeddingClient()
        r1 = await client.embed(["deterministic text"])
        r2 = await client.embed(["deterministic text"])
        assert r1 == r2

    @pytest.mark.asyncio
    async def test_different_text_produces_different_vectors(self):
        client = FakeEmbeddingClient()
        r1 = await client.embed(["text A"])
        r2 = await client.embed(["text B"])
        assert r1[0] != r2[0]

    @pytest.mark.asyncio
    async def test_batch_embed_returns_one_vector_per_text(self):
        client = FakeEmbeddingClient()
        texts = ["alpha", "beta", "gamma"]
        result = await client.embed(texts)
        assert len(result) == len(texts)

    @pytest.mark.asyncio
    async def test_vectors_are_normalised(self):
        """FakeEmbeddingClient produces unit-length (L2-normalised) vectors."""
        client = FakeEmbeddingClient()
        [vec] = await client.embed(["normalise me"])
        norm = sum(x * x for x in vec) ** 0.5
        assert abs(norm - 1.0) < 1e-6

    def test_fake_is_instance_of_embedding_provider(self):
        """FakeEmbeddingClient must satisfy the EmbeddingProvider ABC."""
        client = FakeEmbeddingClient()
        assert isinstance(client, EmbeddingProvider)


# ── get_embedding_provider factory tests ──────────────────────────────────────


class TestGetEmbeddingProviderFactory:
    """Tests that the factory reads EMBEDDING_PROVIDER and returns correct types."""

    def test_default_is_local_when_env_var_unset(self, monkeypatch):
        """
        When EMBEDDING_PROVIDER is not set, the factory should return a
        LocalEmbeddingProvider. We mock the SentenceTransformer import
        so this test runs without the ~90 MB model download.
        """
        monkeypatch.delenv("EMBEDDING_PROVIDER", raising=False)

        mock_st = MagicMock()
        mock_st.return_value = MagicMock()

        with patch.dict("sys.modules", {"sentence_transformers": MagicMock(SentenceTransformer=mock_st)}):
            provider = get_embedding_provider()

        assert isinstance(provider, LocalEmbeddingProvider)

    def test_fake_provider_selected_by_env_var(self, monkeypatch):
        """EMBEDDING_PROVIDER=fake → FakeEmbeddingClient, no external calls."""
        monkeypatch.setenv("EMBEDDING_PROVIDER", "fake")
        provider = get_embedding_provider()
        assert isinstance(provider, FakeEmbeddingClient)

    def test_openai_provider_raises_without_api_key(self, monkeypatch):
        """EMBEDDING_PROVIDER=openai without OPENAI_API_KEY must raise ValueError."""
        monkeypatch.setenv("EMBEDDING_PROVIDER", "openai")
        monkeypatch.delenv("OPENAI_API_KEY", raising=False)
        with pytest.raises(ValueError, match="OPENAI_API_KEY"):
            get_embedding_provider()

    def test_gemini_provider_raises_without_api_key(self, monkeypatch):
        """EMBEDDING_PROVIDER=gemini without GEMINI_API_KEY must raise ValueError."""
        monkeypatch.setenv("EMBEDDING_PROVIDER", "gemini")
        monkeypatch.delenv("GEMINI_API_KEY", raising=False)
        with pytest.raises(ValueError, match="GEMINI_API_KEY"):
            get_embedding_provider()

    def test_unknown_provider_raises_value_error(self, monkeypatch):
        """An unknown provider name must raise ValueError with a useful message."""
        monkeypatch.setenv("EMBEDDING_PROVIDER", "unknown_provider")
        with pytest.raises(ValueError, match="unknown_provider"):
            get_embedding_provider()

    def test_env_var_is_case_insensitive(self, monkeypatch):
        """EMBEDDING_PROVIDER should be normalised to lowercase before matching."""
        monkeypatch.setenv("EMBEDDING_PROVIDER", "FAKE")
        provider = get_embedding_provider()
        assert isinstance(provider, FakeEmbeddingClient)


# ── Retrieval business_id scoping tests ───────────────────────────────────────


class TestRetrievalBusinessIdScoping:
    """
    Tests that ``retrieve_top_k`` always includes business_id in the SQL query.
    We mock psycopg2.connect to avoid a real DB.
    """

    @pytest.mark.asyncio
    async def test_business_id_always_in_query_params(self):
        """The SQL must filter by business_id — cross-tenant leakage is a security failure."""
        business_id = uuid.uuid4()
        fake_client = FakeEmbeddingClient()

        mock_cursor = MagicMock()
        mock_cursor.__enter__ = MagicMock(return_value=mock_cursor)
        mock_cursor.__exit__ = MagicMock(return_value=False)
        mock_cursor.fetchall.return_value = []

        mock_conn = MagicMock()
        mock_conn.__enter__ = MagicMock(return_value=mock_conn)
        mock_conn.__exit__ = MagicMock(return_value=False)
        mock_conn.cursor.return_value = mock_cursor

        with patch("retrieval.psycopg2.connect", return_value=mock_conn):
            with patch("retrieval.psycopg2.extras.register_uuid"):
                from retrieval import retrieve_top_k

                result = await retrieve_top_k(
                    conn_str="postgresql://fake/db",
                    business_id=business_id,
                    question="What are the fees?",
                    embedding_client=fake_client,
                    top_k=4,
                )

        # Result must be empty list (mock returns nothing)
        assert result == []

        # Verify the execute call included business_id in the params
        execute_calls = mock_cursor.execute.call_args_list
        assert len(execute_calls) == 1
        _, params = execute_calls[0].args
        # business_id is the 2nd param (index 1)
        assert str(business_id) in params

    @pytest.mark.asyncio
    async def test_empty_result_is_valid_not_an_error(self):
        """An empty result set (no matching chunks) must return [] not raise."""
        fake_client = FakeEmbeddingClient()

        mock_cursor = MagicMock()
        mock_cursor.__enter__ = MagicMock(return_value=mock_cursor)
        mock_cursor.__exit__ = MagicMock(return_value=False)
        mock_cursor.fetchall.return_value = []

        mock_conn = MagicMock()
        mock_conn.__enter__ = MagicMock(return_value=mock_conn)
        mock_conn.__exit__ = MagicMock(return_value=False)
        mock_conn.cursor.return_value = mock_cursor

        with patch("retrieval.psycopg2.connect", return_value=mock_conn):
            with patch("retrieval.psycopg2.extras.register_uuid"):
                from retrieval import retrieve_top_k

                result = await retrieve_top_k(
                    conn_str="postgresql://fake/db",
                    business_id=uuid.uuid4(),
                    question="irrelevant",
                    embedding_client=fake_client,
                )

        assert result == []
