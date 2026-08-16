"""Application settings — loaded from environment variables or .env file.

Provider selection is done via the ``EMBEDDING_PROVIDER`` environment variable
(read directly in ``embeddings.get_embedding_provider()``), not via this settings
object, so that the factory can be imported and called without requiring the full
FastAPI settings stack (useful in scripts, tests, etc.).
"""

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Configuration for the NeverMissLead RAG microservice."""

    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8")

    # PostgreSQL connection (direct access to kb_chunks only — vector table).
    # All other DB access goes through the .NET API.
    database_url: str = "postgresql://postgres:postgres@localhost:5433/nevermisslead"

    # ── Embedding provider ────────────────────────────────────────────────────
    # Provider selection uses the EMBEDDING_PROVIDER env var directly
    # (see embeddings.get_embedding_provider()).
    # Stored here only for reference / logging; the factory reads os.environ.
    #
    # local   → LocalEmbeddingProvider  (sentence-transformers, 384 dims, free)
    # openai  → OpenAIEmbeddingProvider (requires OPENAI_API_KEY, 1536 dims)
    # gemini  → GeminiEmbeddingProvider (requires GEMINI_API_KEY,  768 dims)
    # fake    → FakeEmbeddingClient     (tests only, 384 dims)
    embedding_provider: str = "local"

    # API keys — read from environment/secrets only, never hardcoded.
    # The factory (get_embedding_provider) reads these directly from os.environ.
    openai_api_key: str = ""
    openai_embedding_model: str = "text-embedding-3-small"
    gemini_api_key: str = ""
    gemini_embedding_model: str = "text-embedding-004"

    # RAG retrieval defaults
    default_top_k: int = 4


settings = Settings()
