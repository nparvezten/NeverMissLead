using Microsoft.EntityFrameworkCore;
using NeverMissLead.Domain.Entities;

namespace NeverMissLead.Application.Interfaces;

/// <summary>
/// Abstraction over the EF Core DbContext used by Application-layer handlers.
/// Keeps Application independent of Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Business> Businesses { get; }
    DbSet<BusinessSettings> BusinessSettings { get; }
    DbSet<KbDocument> KbDocuments { get; }
    DbSet<KbChunk> KbChunks { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<Message> Messages { get; }
    DbSet<Lead> Leads { get; }
    DbSet<FollowUpTask> FollowUpTasks { get; }

    /// <summary>Persists all pending changes to the database.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
