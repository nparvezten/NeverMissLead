using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Infrastructure.Security;

/// <summary>
/// Implements JWT token generation with HMAC-SHA256 signing for owner authentication.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _defaultExpiryMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:SecretKey"] 
            ?? "NeverMissLead_DevSecretKey_AtLeast32BytesLong_2026!";
        _issuer = configuration["Jwt:Issuer"] ?? "NeverMissLead";
        _audience = configuration["Jwt:Audience"] ?? "NeverMissLead";
        _defaultExpiryMinutes = configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 60;
    }

    public string GenerateToken(Guid businessId, string email, string businessName, int? expireMinutes = null)
    {
        var duration = expireMinutes ?? _defaultExpiryMinutes;
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, businessId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("business_id", businessId.ToString()),
            new("business_name", businessName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(duration),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
