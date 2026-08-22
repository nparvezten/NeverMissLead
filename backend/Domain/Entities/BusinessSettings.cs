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
    public List<string> AllowedOrigins { get; private set; } = [];
    public string? PasswordHash { get; private set; }

    // Navigation
    public Business Business { get; private set; } = null!;

    // EF Core constructor
    private BusinessSettings() { }

    /// <summary>Creates settings for a business with sensible defaults.</summary>
    public static BusinessSettings Create(
        Guid businessId,
        string handoffEmail,
        string widgetGreeting = "Hi! How can I help you today?",
        string brandColor = "#6366f1",
        IEnumerable<string>? allowedOrigins = null,
        string? passwordHash = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handoffEmail);

        var settings = new BusinessSettings
        {
            BusinessId = businessId,
            HandoffEmail = handoffEmail.Trim(),
            WidgetGreeting = widgetGreeting.Trim(),
            BrandColor = brandColor.Trim(),
            AllowedOrigins = allowedOrigins?.Select(o => o.Trim().TrimEnd('/')).Distinct().ToList() ?? ["http://localhost:4200"],
            PasswordHash = passwordHash
        };

        return settings;
    }

    /// <summary>Sets the PBKDF2 password hash for owner authentication.</summary>
    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash.Trim();
    }

    /// <summary>Updates the allowed CORS origins for the business widget.</summary>
    public void SetAllowedOrigins(IEnumerable<string> origins)
    {
        AllowedOrigins = origins.Select(o => o.Trim().TrimEnd('/')).Distinct().ToList();
    }
}
