"""
NeverMissLead RAG Microservice — FastAPI entry point.

Endpoints (Week 2):
    POST /embed   — Chunks + embeds a document, writes to kb_chunks.
    POST /query   — Retrieves top-k chunks for a question (business-scoped).

Generation (LLM call) is Week 3.
"""

import uuid
from contextlib import asynccontextmanager
from typing import Annotated

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from chunking import chunk_text
from config import settings
from embeddings import IEmbeddingClient, get_embedding_client
from retrieval import RetrievedChunk, embed_and_store_chunks, retrieve_top_k


# ── Startup / shutdown ────────────────────────────────────────────────────────


@asynccontextmanager
async def lifespan(app: FastAPI):
    """Initialise the embedding client once at startup."""
    app.state.embedding_client = get_embedding_client(
        provider=settings.embedding_provider,
        api_key=settings.openai_api_key,
        model=settings.openai_embedding_model,
    )
    yield


app = FastAPI(
    title="NeverMissLead RAG Service",
    description=(
        "Chunking, embedding, and retrieval microservice for NeverMissLead. "
        "Accessed by the .NET backend only — not exposed to the public internet."
    ),
    version="0.1.0",
    lifespan=lifespan,
)


# ── Request / Response models ─────────────────────────────────────────────────


class EmbedRequest(BaseModel):
    """Request to chunk and embed a knowledge document."""

    business_id: uuid.UUID = Field(..., description="Tenant ID — document belongs to this business.")
    document_id: uuid.UUID = Field(..., description="PK of the kb_documents row already inserted by .NET API.")
    text: str = Field(..., min_length=1, max_length=1_000_000, description="Raw document text.")
    chunk_size: int = Field(400, ge=100, le=1000, description="Target tokens per chunk.")
    overlap: int = Field(50, ge=0, le=200, description="Token overlap between consecutive chunks.")


class EmbedResponse(BaseModel):
    chunk_ids: list[uuid.UUID]
    chunks_created: int


class QueryRequest(BaseModel):
    """Request to retrieve the most relevant chunks for a visitor question."""

    business_id: uuid.UUID = Field(..., description="Tenant boundary — only this business's chunks are searched.")
    question: str = Field(..., min_length=1, max_length=2000, description="Visitor question text.")
    top_k: int = Field(4, ge=1, le=20, description="Number of chunks to return.")


class ChunkResult(BaseModel):
    chunk_id: uuid.UUID
    chunk_text: str
    score: float = Field(..., description="Cosine similarity score (0–1, higher = more relevant).")


class QueryResponse(BaseModel):
    chunks: list[ChunkResult]


# ── Endpoints ─────────────────────────────────────────────────────────────────


@app.post("/embed", response_model=EmbedResponse, status_code=201)
async def embed_document(request: EmbedRequest) -> EmbedResponse:
    """
    Chunks the raw document text, generates embeddings for each chunk,
    and writes them to the ``kb_chunks`` table.

    The ``kb_documents`` row must already exist (created by the .NET API).
    """
    embedding_client: IEmbeddingClient = app.state.embedding_client

    chunks = chunk_text(request.text, chunk_size=request.chunk_size, overlap=request.overlap)
    if not chunks:
        raise HTTPException(status_code=422, detail="Document text produced no chunks.")

    chunk_tuples = [(c.text, c.token_count) for c in chunks]

    try:
        chunk_ids = await embed_and_store_chunks(
            conn_str=settings.database_url,
            document_id=request.document_id,
            business_id=request.business_id,
            chunks=chunk_tuples,
            embedding_client=embedding_client,
        )
    except Exception as exc:
        raise HTTPException(status_code=500, detail=f"Failed to store chunks: {exc}") from exc

    return EmbedResponse(chunk_ids=chunk_ids, chunks_created=len(chunk_ids))


@app.post("/query", response_model=QueryResponse)
async def query_chunks(request: QueryRequest) -> QueryResponse:
    """
    Embeds the visitor's question and returns the top-k most relevant
    KB chunks, scoped strictly to the specified business.

    An empty result (no chunks found) is valid — the caller should flag
    ``needs_human`` when no relevant chunks are returned.
    """
    embedding_client: IEmbeddingClient = app.state.embedding_client

    try:
        results: list[RetrievedChunk] = await retrieve_top_k(
            conn_str=settings.database_url,
            business_id=request.business_id,
            question=request.question,
            embedding_client=embedding_client,
            top_k=request.top_k,
        )
    except Exception as exc:
        raise HTTPException(status_code=500, detail=f"Retrieval failed: {exc}") from exc

    return QueryResponse(
        chunks=[
            ChunkResult(chunk_id=r.chunk_id, chunk_text=r.chunk_text, score=r.score)
            for r in results
        ]
    )


@app.get("/health")
async def health() -> dict:
    """Liveness probe for the RAG service."""
    return {"status": "ok", "service": "nevermisslead-rag"}
