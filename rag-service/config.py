"""Application settings — loaded from environment variables or .env file."""

from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Configuration for the NeverMissLead RAG microservice."""

    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8")

    # PostgreSQL connection (direct access to kb_chunks only — vector table)
    database_url: str = (
        "postgresql://parvezkhan@localhost:5432/nevermisslead"
    )

    # Embedding provider — kept behind an interface (IEmbeddingClient) in code.
    # Set to "fake" in tests to avoid real API calls.
    embedding_provider: str = "openai"
    openai_api_key: str = ""
    openai_embedding_model: str = "text-embedding-3-small"

    # RAG retrieval defaults
    default_top_k: int = 4


settings = Settings()
