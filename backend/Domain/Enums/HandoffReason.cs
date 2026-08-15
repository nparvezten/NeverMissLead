namespace NeverMissLead.Domain.Enums;

/// <summary>Reason a conversation was flagged for human handoff.</summary>
public enum HandoffReason
{
    None = 0,
    NoCitedChunks = 1,
    LowConfidence = 2,
    ExplicitRequest = 3,
    OutOfScope = 4
}
