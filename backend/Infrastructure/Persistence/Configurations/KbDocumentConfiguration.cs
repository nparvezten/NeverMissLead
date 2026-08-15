using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Infrastructure.Persistence.Configurations;

internal sealed class KbDocumentConfiguration : IEntityTypeConfiguration<KbDocument>
{
    public void Configure(EntityTypeBuilder<KbDocument> builder)
    {
        builder.ToTable("kb_documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.BusinessId).IsRequired();
        builder.Property(d => d.Title).HasMaxLength(500).IsRequired();
        builder.Property(d => d.SourceType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(d => d.RawText).IsRequired();
        builder.Property(d => d.UploadedAt).IsRequired();

        builder.HasMany(d => d.Chunks)
            .WithOne(c => c.Document)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
