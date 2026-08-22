using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NeverMissLead.API.Middleware;
using NeverMissLead.Application.Extensions;
using NeverMissLead.Infrastructure.Extensions;
using NeverMissLead.Infrastructure.Persistence;
using Serilog;
using System.Threading.RateLimiting;

// ── Bootstrap Serilog before the host builds ──────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting NeverMissLead API");
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog (replaces default logging) ────────────────────────────────────
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}")
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "NeverMissLead.API"));

    // ── Application + Infrastructure layers ───────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── Controllers + RFC7807 ProblemDetails ──────────────────────────────────
    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            // Use our global exception middleware for unhandled errors
            options.SuppressMapClientErrors = false;
        });

    builder.Services.AddProblemDetails();

    // ── Rate limiting (widget and auth endpoints — internet-facing) ──────────
    builder.Services.AddRateLimiter(rl =>
    {
        rl.AddFixedWindowLimiter("widget", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 30;
            opt.QueueLimit = 5;
            opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        });

        // Strict rate limiter for authentication endpoints to prevent brute-force attacks
        rl.AddFixedWindowLimiter("login", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 5;
            opt.QueueLimit = 0;
            opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        });

        rl.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // ── Authentication & Authorization (JWT via HttpOnly Cookie / Header) ────
    var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];
    if (builder.Environment.IsProduction() && (string.IsNullOrEmpty(jwtSecretKey) || jwtSecretKey.Contains("DevSecretKey")))
    {
        throw new InvalidOperationException("A cryptographically secure, non-default Jwt:SecretKey must be configured in Production environment.");
    }
    jwtSecretKey ??= "NeverMissLead_DevSecretKey_AtLeast32BytesLong_2026!";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Enabled for local dev/testing
        options.SaveToken = true;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "NeverMissLead",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "NeverMissLead",
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtSecretKey)),
            ClockSkew = TimeSpan.Zero
        };

        // Extract JWT from HttpOnly 'nml_token' cookie if not in Authorization header
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token) && context.Request.Cookies.TryGetValue("nml_token", out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // ── Dynamic CORS (Per-business allowed origins for widget, frontend for dashboard) ───
    builder.Services.AddCors();
    builder.Services.AddSingleton<Microsoft.AspNetCore.Cors.Infrastructure.ICorsPolicyProvider, NeverMissLead.API.Cors.DynamicCorsPolicyProvider>();

    // ── Health checks ─────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("postgres");

    var app = builder.Build();

    // ── Auto-migrate on startup (dev convenience; use explicit CLI in prod) ────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied");
    }

    // ── Middleware pipeline ───────────────────────────────────────────────────
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionMiddleware>();
    app.UseRouting();
    app.UseCors();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // /health — no auth, no rate limit
    app.MapHealthChecks("/health");
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "NeverMissLead API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program accessible for integration tests
public partial class Program { }
