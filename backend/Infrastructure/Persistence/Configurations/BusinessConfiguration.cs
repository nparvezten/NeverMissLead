using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeverMissLead.Domain.Entities;

namespace NeverMissLead.Infrastructure.Persistence.Configurations;

internal sealed class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("businesses");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Name).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Niche).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Timezone).HasMaxLength(50).IsRequired().HasDefaultValue("UTC");
        builder.Property(b => b.CreatedAt).IsRequired();

        builder.HasOne(b => b.Settings)
            .WithOne(s => s.Business)
            .HasForeignKey<BusinessSettings>(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.KbDocuments)
            .WithOne(d => d.Business)
            .HasForeignKey(d => d.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(b => b.Conversations)
            .WithOne(c => c.Business)
            .HasForeignKey(c => c.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
