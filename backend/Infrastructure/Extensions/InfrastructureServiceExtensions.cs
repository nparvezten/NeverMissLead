using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Infrastructure.Persistence;
using NeverMissLead.Infrastructure.RagClient;

namespace NeverMissLead.Infrastructure.Extensions;

/// <summary>
/// Registers Infrastructure-layer services: EF Core DbContext, HTTP clients,
/// and repository implementations.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core + Npgsql
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // RAG HTTP client — wraps the Python FastAPI service
        services.AddHttpClient<IRagClient, RagHttpClient>(client =>
        {
            var baseUrl = configuration["RagService:BaseUrl"]
                ?? "http://localhost:8000";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
