using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NeverMissLead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPgvectorExtensionAndEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable the pgvector extension (requires the extension to be installed on the server)
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            // Add the embedding column to kb_chunks — vector(1536) matches OpenAI text-embedding-3-small
            migrationBuilder.Sql(
                "ALTER TABLE kb_chunks ADD COLUMN IF NOT EXISTS embedding vector(1536);");

            // IVFFlat index for approximate cosine-distance retrieval
            // lists=100 is appropriate for up to ~1 million vectors; tune per data size
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_kb_chunks_embedding_cosine " +
                "ON kb_chunks USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_kb_chunks_embedding_cosine;");
            migrationBuilder.Sql("ALTER TABLE kb_chunks DROP COLUMN IF EXISTS embedding;");
        }
    }
}
