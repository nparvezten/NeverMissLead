using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Infrastructure.Persistence.Configurations;

namespace NeverMissLead.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for all relational tables.
/// The <c>kb_chunks.embedding</c> column (pgvector <c>vector(384)</c>) is
/// defined via raw migration SQL and is intentionally excluded from this context —
/// the RAG service accesses it directly via Dapper + psycopg2.
/// </summary>
public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<BusinessSettings> BusinessSettings => Set<BusinessSettings>();
    public DbSet<KbDocument> KbDocuments => Set<KbDocument>();
    public DbSet<KbChunk> KbChunks => Set<KbChunk>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<FollowUpTask> FollowUpTasks => Set<FollowUpTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
