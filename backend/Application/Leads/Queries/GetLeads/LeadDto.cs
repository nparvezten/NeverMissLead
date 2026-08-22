namespace NeverMissLead.Application.Leads.Queries.GetLeads;

public sealed record LeadDto(
    Guid Id,
    Guid ConversationId,
    Guid BusinessId,
    string? Name,
    string? Phone,
    string? Email,
    string? IntentSummary,
    int QualificationScore,
    string Status,
    DateTime CreatedAt);
