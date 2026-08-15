using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Domain.Enums;

namespace NeverMissLead.Infrastructure.Persistence.Configurations;

internal sealed class FollowUpTaskConfiguration : IEntityTypeConfiguration<FollowUpTask>
{
    public void Configure(EntityTypeBuilder<FollowUpTask> builder)
    {
        builder.ToTable("follow_up_tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.LeadId).IsRequired();
        builder.Property(t => t.ScheduledFor).IsRequired();
        builder.Property(t => t.Channel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(t => t.MessageDraft).HasMaxLength(2000).IsRequired();
        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(t => t.SentAt);

        // Index for the hosted-service poller query: scheduled_for <= now AND status = 'Pending'
        builder.HasIndex(t => new { t.ScheduledFor, t.Status })
            .HasDatabaseName("ix_follow_up_tasks_scheduled_status");
    }
}
