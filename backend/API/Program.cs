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

    // ── Rate limiting (widget endpoint — unauthenticated, internet-facing) ─────
    builder.Services.AddRateLimiter(rl =>
    {
        rl.AddFixedWindowLimiter("widget", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 30;
            opt.QueueLimit = 5;
            opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        });
        rl.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // ── CORS (allow all for widget in v1) ────────────────────────────────────
    // TODO Week 4: restrict to registered business origins configured in business_settings
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("WidgetCorsPolicy", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

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
    app.UseCors("WidgetCorsPolicy");
    app.UseRateLimiter();
    app.UseRouting();

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
