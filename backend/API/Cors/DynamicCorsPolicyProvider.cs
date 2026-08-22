using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NeverMissLead.Infrastructure.Persistence;

namespace NeverMissLead.API.Cors;

/// <summary>
/// Dynamic CORS policy provider that validates widget requests against the
/// business's registered allowed origins in PostgreSQL (<c>business_settings.allowed_origins</c>),
/// and allows dashboard requests from configured frontend origins with credentials.
/// </summary>
public sealed class DynamicCorsPolicyProvider : ICorsPolicyProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public DynamicCorsPolicyProvider(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var origin = context.Request.Headers.Origin.ToString().Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(origin))
        {
            return null;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        // Widget endpoints: /api/v1/widget/{businessId}/...
        if (path.StartsWith("/api/v1/widget/", StringComparison.OrdinalIgnoreCase))
        {
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            // Expected segments: ["api", "v1", "widget", "{businessId}", ...]
            if (segments.Length >= 4 && Guid.TryParse(segments[3], out var businessId))
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var settings = await db.BusinessSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.BusinessId == businessId);

                if (settings != null && settings.AllowedOrigins.Any(o => o.TrimEnd('/').Equals(origin, StringComparison.OrdinalIgnoreCase)))
                {
                    return new CorsPolicyBuilder()
                        .WithOrigins(origin)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .Build();
                }

                // Origin not in business's registered list -> return restrictive policy without allow-origin
                return new CorsPolicyBuilder().Build();
            }
        }

        // Dashboard & standard app CORS policy: allow frontend with HttpOnly cookie credentials
        var frontendOrigin = _configuration["Frontend:Origin"] ?? "http://localhost:4200";

        var builder = new CorsPolicyBuilder()
            .WithOrigins(frontendOrigin.TrimEnd('/'), "http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();

        return builder.Build();
    }
}
