using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeverMissLead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixEmbeddingDimensionTo384 : Migration
    {
        /// <summary>
        /// Fixes the embedding column from vector(1536) — which was set for OpenAI
        /// text-embedding-3-small — to vector(384), which matches the default
        /// LocalEmbeddingProvider (sentence-transformers all-MiniLM-L6-v2, CPU, free).
        ///
        /// Per AGENTS.md: v1 fixes on one dimension for local dev. Switching
        /// providers later (e.g. to OpenAI 1536-dim) requires re-embedding all
        /// kb_chunks rows — this is documented, expected behaviour.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the old IVFFlat index — cannot alter dimension in-place.
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS ix_kb_chunks_embedding_cosine;");

            // Drop and re-add the embedding column with the correct dimension.
            // We use DROP + ADD instead of ALTER TYPE because pgvector does not
            // support changing the dimension of an existing vector column.
            migrationBuilder.Sql(
                "ALTER TABLE kb_chunks DROP COLUMN IF EXISTS embedding;");

            migrationBuilder.Sql(
                "ALTER TABLE kb_chunks ADD COLUMN embedding vector(384);");

            // Recreate the IVFFlat index for cosine-distance retrieval.
            // lists=100 is appropriate for up to ~1 million vectors.
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_kb_chunks_embedding_cosine " +
                "ON kb_chunks USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX IF EXISTS ix_kb_chunks_embedding_cosine;");

            migrationBuilder.Sql(
                "ALTER TABLE kb_chunks DROP COLUMN IF EXISTS embedding;");

            migrationBuilder.Sql(
                "ALTER TABLE kb_chunks ADD COLUMN embedding vector(1536);");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_kb_chunks_embedding_cosine " +
                "ON kb_chunks USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);");
        }
    }
}
