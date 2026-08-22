namespace NeverMissLead.Application.Interfaces;

/// <summary>
/// Service for generating signed JWT tokens for business owner authentication.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT token containing owner and business identity claims.
    /// </summary>
    string GenerateToken(Guid businessId, string email, string businessName, int? expireMinutes = null);
}
