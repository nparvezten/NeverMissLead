namespace NeverMissLead.Domain.Entities;

/// <summary>
/// Configurable presentation and contact settings for a business's chat widget.
/// One-to-one with <see cref="Business"/>.
/// </summary>
public class BusinessSettings
{
    public Guid BusinessId { get; private set; }
    public string WidgetGreeting { get; private set; } = "Hi! How can I help you today?";
    public string HandoffEmail { get; private set; } = string.Empty;
    public string BrandColor { get; private set; } = "#6366f1";

    // Navigation
    public Business Business { get; private set; } = null!;

    // EF Core constructor
    private BusinessSettings() { }

    /// <summary>Creates settings for a business with sensible defaults.</summary>
    public static BusinessSettings Create(
        Guid businessId,
        string handoffEmail,
        string widgetGreeting = "Hi! How can I help you today?",
        string brandColor = "#6366f1")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handoffEmail);

        return new BusinessSettings
        {
            BusinessId = businessId,
            HandoffEmail = handoffEmail.Trim(),
            WidgetGreeting = widgetGreeting.Trim(),
            BrandColor = brandColor.Trim()
        };
    }
}
