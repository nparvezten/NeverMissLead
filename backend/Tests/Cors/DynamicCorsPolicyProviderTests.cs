using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeverMissLead.API.Cors;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace NeverMissLead.Tests.Cors;

public sealed class DynamicCorsPolicyProviderTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly Guid _registeredBusinessId;
    private readonly Guid _otherBusinessId;

    public DynamicCorsPolicyProviderTests()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(dbName));

        _serviceProvider = services.BuildServiceProvider();
        _db = _serviceProvider.GetRequiredService<ApplicationDbContext>();

        var configValues = new Dictionary<string, string?>
        {
            ["Frontend:Origin"] = "http://localhost:4200"
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();

        // Seed registered business with localhost:4200 and client-app.com
        var b1 = Business.Create("Bright Minds Coaching", "private tutors");
        _registeredBusinessId = b1.Id;
        var s1 = BusinessSettings.Create(
            b1.Id,
            "owner@brightminds.test",
            allowedOrigins: ["http://localhost:4200", "https://brightminds.edu"]);
        _db.Businesses.Add(b1);
        _db.BusinessSettings.Add(s1);

        // Seed second business with different origins
        var b2 = Business.Create("Ace Tutors", "math tutors");
        _otherBusinessId = b2.Id;
        var s2 = BusinessSettings.Create(
            b2.Id,
            "owner@acetutors.test",
            allowedOrigins: ["https://acetutors.com"]);
        _db.Businesses.Add(b2);
        _db.BusinessSettings.Add(s2);

        _db.SaveChanges();
    }

    [Fact]
    public async Task WidgetRequest_RegisteredOrigin_ReturnsAllowedCorsPolicy()
    {
        // Arrange
        var provider = new DynamicCorsPolicyProvider(_serviceProvider, _configuration);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = $"/api/v1/widget/{_registeredBusinessId}/chat";
        httpContext.Request.Headers["Origin"] = "http://localhost:4200";

        // Act
        var policy = await provider.GetPolicyAsync(httpContext, null);

        // Assert
        policy.ShouldNotBeNull();
        policy.Origins.ShouldContain("http://localhost:4200");
        policy.AllowAnyHeader.ShouldBeTrue();
        policy.AllowAnyMethod.ShouldBeTrue();
    }

    [Fact]
    public async Task WidgetRequest_UnregisteredOrigin_RejectsOrigin()
    {
        // Arrange
        var provider = new DynamicCorsPolicyProvider(_serviceProvider, _configuration);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = $"/api/v1/widget/{_registeredBusinessId}/chat";
        httpContext.Request.Headers["Origin"] = "https://unauthorized-evil-site.com";

        // Act
        var policy = await provider.GetPolicyAsync(httpContext, null);

        // Assert
        policy.ShouldNotBeNull();
        policy.Origins.ShouldNotContain("https://unauthorized-evil-site.com");
        policy.Origins.Count.ShouldBe(0);
    }

    [Fact]
    public async Task DashboardRequest_FrontendOrigin_AllowsCredentials()
    {
        // Arrange
        var provider = new DynamicCorsPolicyProvider(_serviceProvider, _configuration);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/v1/dashboard/leads";
        httpContext.Request.Headers["Origin"] = "http://localhost:4200";

        // Act
        var policy = await provider.GetPolicyAsync(httpContext, null);

        // Assert
        policy.ShouldNotBeNull();
        policy.Origins.ShouldContain("http://localhost:4200");
        policy.SupportsCredentials.ShouldBeTrue();
    }

    public void Dispose()
    {
        _db.Dispose();
        _serviceProvider.Dispose();
    }
}
