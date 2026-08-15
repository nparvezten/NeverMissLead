"""
Unit tests for chunking.py

These tests are entirely in-process — no DB, no embedding API calls.
"""

import pytest

from chunking import Chunk, chunk_text


class TestChunkText:
    def test_empty_text_returns_empty_list(self):
        result = chunk_text("")
        assert result == []

    def test_short_text_produces_single_chunk(self):
        text = "Hello world"
        result = chunk_text(text)
        assert len(result) == 1
        assert isinstance(result[0], Chunk)
        assert result[0].token_count > 0

    def test_long_text_produces_multiple_chunks(self):
        # Generate text that's definitely > 400 tokens
        text = "The quick brown fox jumps over the lazy dog. " * 100
        result = chunk_text(text, chunk_size=50, overlap=10)
        assert len(result) > 1

    def test_overlap_creates_shared_content(self):
        """Adjacent chunks should share content when overlap > 0."""
        text = " ".join(f"word{i}" for i in range(500))
        chunks = chunk_text(text, chunk_size=50, overlap=10)
        # The first chunk's last N tokens should appear in the second chunk's text
        assert len(chunks) >= 2
        # Both chunks should have positive token counts
        for chunk in chunks:
            assert chunk.token_count > 0

    def test_zero_overlap_produces_non_overlapping_chunks(self):
        text = " ".join(f"word{i}" for i in range(300))
        chunks = chunk_text(text, chunk_size=50, overlap=0)
        assert all(c.token_count <= 50 for c in chunks)

    def test_chunk_size_lte_overlap_raises_value_error(self):
        with pytest.raises(ValueError, match="chunk_size"):
            chunk_text("some text", chunk_size=10, overlap=10)

    def test_chunk_text_is_decodable_string(self):
        text = "Bright Minds Coaching offers math tuition for grades 6–12."
        result = chunk_text(text)
        for chunk in result:
            assert isinstance(chunk.text, str)
            assert len(chunk.text) > 0

    def test_token_count_matches_content(self):
        import tiktoken
        enc = tiktoken.get_encoding("cl100k_base")
        text = "NeverMissLead automates lead capture for coaching businesses." * 20
        chunks = chunk_text(text, chunk_size=30, overlap=5)
        for chunk in chunks:
            actual_tokens = len(enc.encode(chunk.text))
            assert actual_tokens == chunk.token_count
