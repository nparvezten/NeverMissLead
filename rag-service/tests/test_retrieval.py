"""
Unit tests for retrieval.py — specifically the business_id scoping invariant
and the FakeEmbeddingClient behaviour.

These tests mock the DB connection and never hit a real Postgres instance.
"""

import uuid
from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from embeddings import FakeEmbeddingClient


class TestFakeEmbeddingClient:
    """Tests for the deterministic fake used in all other unit tests."""

    @pytest.mark.asyncio
    async def test_returns_correct_dimension(self):
        client = FakeEmbeddingClient()
        result = await client.embed(["hello world"])
        assert len(result) == 1
        assert len(result[0]) == FakeEmbeddingClient.DIMENSIONS

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
        """FakeEmbeddingClient normalises to unit vectors."""
        client = FakeEmbeddingClient()
        [vec] = await client.embed(["normalise me"])
        norm = sum(x * x for x in vec) ** 0.5
        assert abs(norm - 1.0) < 1e-6


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
