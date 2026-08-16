"""
Retrieval module — cosine-distance top-k search against the ``kb_chunks`` table.

Security invariant:
    Every query is filtered by ``business_id``. Cross-tenant retrieval
    is architecturally impossible — the WHERE clause is always present.

Storage note:
    This module accesses the ``kb_chunks`` table directly via psycopg2
    (pgvector raw SQL) and also writes chunks + embeddings during ingestion.
    All other DB access goes through the .NET API.
"""

import uuid
from dataclasses import dataclass

import psycopg2
import psycopg2.extras

from embeddings import EmbeddingProvider


@dataclass(frozen=True)
class RetrievedChunk:
    """A single chunk returned by a similarity search."""

    chunk_id: uuid.UUID
    chunk_text: str
    score: float  # cosine similarity (higher = more relevant)


def _vec_to_pg(vec: list[float]) -> str:
    """Converts a Python float list to the pgvector literal format ``[x1,x2,...]``."""
    return "[" + ",".join(str(x) for x in vec) + "]"


async def embed_and_store_chunks(
    conn_str: str,
    document_id: uuid.UUID,
    business_id: uuid.UUID,
    chunks: list[tuple[str, int]],  # (chunk_text, token_count)
    embedding_client: EmbeddingProvider,
) -> list[uuid.UUID]:
    """
    Embeds a list of chunks and writes them to ``kb_chunks``.

    Args:
        conn_str:         Postgres DSN (direct connection to the DB).
        document_id:      FK to ``kb_documents``.
        business_id:      Used only for logging; referential integrity via document.
        chunks:           List of (text, token_count) tuples from the chunker.
        embedding_client: Produces the embedding vectors.

    Returns:
        List of newly inserted chunk UUIDs.
    """
    texts = [text for text, _ in chunks]
    embeddings = await embedding_client.embed(texts)

    chunk_ids: list[uuid.UUID] = []

    with psycopg2.connect(conn_str) as conn:
        with conn.cursor() as cur:
            for (text, token_count), embedding in zip(chunks, embeddings):
                chunk_id = uuid.uuid4()
                cur.execute(
                    """
                    INSERT INTO kb_chunks (id, document_id, chunk_text, token_count, embedding)
                    VALUES (%s, %s, %s, %s, %s::vector)
                    """,
                    (str(chunk_id), str(document_id), text, token_count, _vec_to_pg(embedding)),
                )
                chunk_ids.append(chunk_id)
        conn.commit()

    return chunk_ids


async def retrieve_top_k(
    conn_str: str,
    business_id: uuid.UUID,
    question: str,
    embedding_client: EmbeddingProvider,
    top_k: int = 4,
) -> list[RetrievedChunk]:
    """
    Embeds *question* and returns the top-k most similar chunks for *business_id*.

    Security: the WHERE clause always filters by business_id (via document join).
    No cross-tenant data is ever returned, even if pgvector returns more candidates.

    Args:
        conn_str:         Postgres DSN.
        business_id:      The tenant boundary — enforced at the SQL level.
        question:         Raw visitor question text.
        embedding_client: Produces the query embedding.
        top_k:            Number of results to return.

    Returns:
        List of :class:`RetrievedChunk` sorted by descending cosine similarity.
    """
    [query_embedding] = await embedding_client.embed([question])

    results: list[RetrievedChunk] = []

    with psycopg2.connect(conn_str) as conn:
        psycopg2.extras.register_uuid(conn)
        with conn.cursor() as cur:
            cur.execute(
                """
                SELECT
                    kc.id,
                    kc.chunk_text,
                    1 - (kc.embedding <=> %s::vector) AS cosine_similarity
                FROM kb_chunks kc
                JOIN kb_documents kd ON kd.id = kc.document_id
                WHERE kd.business_id = %s
                  AND kc.embedding IS NOT NULL
                ORDER BY kc.embedding <=> %s::vector
                LIMIT %s
                """,
                (
                    _vec_to_pg(query_embedding),
                    str(business_id),
                    _vec_to_pg(query_embedding),
                    top_k,
                ),
            )
            for row in cur.fetchall():
                results.append(
                    RetrievedChunk(
                        chunk_id=uuid.UUID(str(row[0])),
                        chunk_text=row[1],
                        score=float(row[2]),
                    )
                )

    return results
