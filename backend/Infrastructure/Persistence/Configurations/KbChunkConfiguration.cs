using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeverMissLead.Domain.Entities;

namespace NeverMissLead.Infrastructure.Persistence.Configurations;

internal sealed class KbChunkConfiguration : IEntityTypeConfiguration<KbChunk>
{
    public void Configure(EntityTypeBuilder<KbChunk> builder)
    {
        builder.ToTable("kb_chunks");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.DocumentId).IsRequired();
        builder.Property(c => c.ChunkText).IsRequired();
        builder.Property(c => c.TokenCount).IsRequired();

        // The embedding vector(384) column is added via raw SQL in the migration
        // (see FixEmbeddingDimensionTo384 migration).
        // EF Core does not manage it — reads/writes go through the Python RAG service.
    }
}
