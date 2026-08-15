using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Businesses.Queries.GetBusiness;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace NeverMissLead.Tests.Businesses;

/// <summary>Unit tests for <see cref="GetBusinessQueryHandler"/>.</summary>
public sealed class GetBusinessQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly GetBusinessQueryHandler _handler;

    public GetBusinessQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _handler = new GetBusinessQueryHandler(_db);
    }

    [Fact]
    public async Task Handle_ExistingBusiness_ReturnsDto()
    {
        // Arrange
        var business = Business.Create("Bright Minds", "tutors", "Asia/Kolkata");
        var settings = BusinessSettings.Create(business.Id, "owner@bright.test");
        _db.Businesses.Add(business);
        _db.BusinessSettings.Add(settings);
        await _db.SaveChangesAsync();

        // Act
        var dto = await _handler.Handle(new GetBusinessQuery(business.Id));

        // Assert
        dto.ShouldNotBeNull();
        dto!.Id.ShouldBe(business.Id);
        dto.Name.ShouldBe("Bright Minds");
        dto.Niche.ShouldBe("tutors");
        dto.WidgetGreeting.ShouldBe("Hi! How can I help you today?");
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNull()
    {
        // Act
        var dto = await _handler.Handle(new GetBusinessQuery(Guid.NewGuid()));

        // Assert
        dto.ShouldBeNull();
    }

    public void Dispose() => _db.Dispose();
}
