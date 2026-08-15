"""
Chunking module — splits raw text into overlapping token-bounded chunks.

Design:
- Uses ``tiktoken`` for token counting (same tokenizer as OpenAI embeddings)
- Chunk size: 300–500 tokens (default 400), overlap: 50 tokens
- Does NOT call any external API — pure in-process computation
"""

from dataclasses import dataclass

import tiktoken

_ENCODING = tiktoken.get_encoding("cl100k_base")  # compatible with text-embedding-3-*


@dataclass(frozen=True)
class Chunk:
    """A single text chunk ready for embedding."""

    text: str
    token_count: int


def chunk_text(
    text: str,
    chunk_size: int = 400,
    overlap: int = 50,
) -> list[Chunk]:
    """
    Splits *text* into overlapping token-based chunks.

    Args:
        text:       Raw document text (plain text, pre-extracted from PDF if needed).
        chunk_size: Target maximum tokens per chunk.
        overlap:    Number of tokens shared between consecutive chunks.

    Returns:
        A list of :class:`Chunk` objects ordered as they appear in the source text.

    Raises:
        ValueError: If chunk_size <= overlap.
    """
    if chunk_size <= overlap:
        raise ValueError(f"chunk_size ({chunk_size}) must be greater than overlap ({overlap})")

    tokens: list[int] = _ENCODING.encode(text)

    if not tokens:
        return []

    chunks: list[Chunk] = []
    step = chunk_size - overlap
    start = 0

    while start < len(tokens):
        end = min(start + chunk_size, len(tokens))
        chunk_tokens = tokens[start:end]
        chunk_text_decoded = _ENCODING.decode(chunk_tokens)
        chunks.append(Chunk(text=chunk_text_decoded, token_count=len(chunk_tokens)))
        if end == len(tokens):
            break
        start += step

    return chunks
