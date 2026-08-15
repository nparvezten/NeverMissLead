using NeverMissLead.Domain.Common;

namespace NeverMissLead.Domain.Entities;

/// <summary>
/// A business (tutor/coaching class) that uses NeverMissLead to capture leads
/// and answer visitor questions from its own knowledge base.
/// </summary>
public class Business
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Niche { get; private set; } = string.Empty;
    public string Timezone { get; private set; } = "UTC";
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public BusinessSettings? Settings { get; private set; }
    public IReadOnlyCollection<KbDocument> KbDocuments => _kbDocuments.AsReadOnly();
    public IReadOnlyCollection<Conversation> Conversations => _conversations.AsReadOnly();

    private readonly List<KbDocument> _kbDocuments = [];
    private readonly List<Conversation> _conversations = [];

    // EF Core constructor
    private Business() { }

    /// <summary>Creates a new business with the required identifying fields.</summary>
    public static Business Create(string name, string niche, string timezone = "UTC")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(niche);

        return new Business
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Niche = niche.Trim(),
            Timezone = timezone.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
