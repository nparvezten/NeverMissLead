using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeverMissLead.Domain.Entities;

namespace NeverMissLead.Infrastructure.Persistence.Configurations;

internal sealed class BusinessSettingsConfiguration : IEntityTypeConfiguration<BusinessSettings>
{
    public void Configure(EntityTypeBuilder<BusinessSettings> builder)
    {
        builder.ToTable("business_settings");
        builder.HasKey(s => s.BusinessId);
        builder.Property(s => s.BusinessId).ValueGeneratedNever();
        builder.Property(s => s.WidgetGreeting).HasMaxLength(500).IsRequired();
        builder.Property(s => s.HandoffEmail).HasMaxLength(320).IsRequired();
        builder.Property(s => s.BrandColor).HasMaxLength(7).IsRequired().HasDefaultValue("#6366f1");
        builder.Property(s => s.AllowedOrigins).HasColumnType("text[]");
        builder.Property(s => s.PasswordHash).HasMaxLength(500);
    }
}
