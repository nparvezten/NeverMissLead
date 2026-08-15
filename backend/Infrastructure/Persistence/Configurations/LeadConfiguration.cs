using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Infrastructure.Persistence.Configurations;

internal sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.ConversationId).IsRequired();
        builder.Property(l => l.BusinessId).IsRequired();

        // AES-256 encrypted PII columns — nullable, encrypted by Infrastructure layer
        builder.Property(l => l.NameEnc).HasColumnName("name_enc").HasMaxLength(512);
        builder.Property(l => l.PhoneEnc).HasColumnName("phone_enc").HasMaxLength(512);
        builder.Property(l => l.EmailEnc).HasColumnName("email_enc").HasMaxLength(512);

        builder.Property(l => l.IntentSummary).HasMaxLength(1000);
        builder.Property(l => l.QualificationScore).IsRequired().HasDefaultValue(0);
        builder.Property(l => l.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(l => l.CreatedAt).IsRequired();

        builder.HasOne(l => l.Conversation)
            .WithMany()
            .HasForeignKey(l => l.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.FollowUpTasks)
            .WithOne(t => t.Lead)
            .HasForeignKey(t => t.LeadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
