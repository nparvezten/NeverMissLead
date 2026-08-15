using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Businesses.Commands.CreateBusiness;
using NeverMissLead.Infrastructure.Persistence;
using Shouldly;
using Xunit;

namespace NeverMissLead.Tests.Businesses;

/// <summary>Unit tests for <see cref="CreateBusinessCommandHandler"/>.</summary>
public sealed class CreateBusinessCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly CreateBusinessCommandHandler _handler;

    public CreateBusinessCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _handler = new CreateBusinessCommandHandler(_db, new CreateBusinessCommandValidator());
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesBusinessAndSettings()
    {
        // Arrange
        var command = new CreateBusinessCommand(
            Name: "Bright Minds Coaching",
            Niche: "private tutors",
            Timezone: "Asia/Kolkata",
            HandoffEmail: "owner@brightminds.test",
            WidgetGreeting: "Hello! Ask me anything about our courses.",
            BrandColor: "#4f46e5");

        // Act
        var id = await _handler.Handle(command);

        // Assert
        id.ShouldNotBe(Guid.Empty);

        var business = await _db.Businesses.Include(b => b.Settings)
            .FirstAsync(b => b.Id == id);

        business.Name.ShouldBe("Bright Minds Coaching");
        business.Niche.ShouldBe("private tutors");
        business.Timezone.ShouldBe("Asia/Kolkata");
        business.Settings.ShouldNotBeNull();
        business.Settings!.HandoffEmail.ShouldBe("owner@brightminds.test");
        business.Settings.BrandColor.ShouldBe("#4f46e5");
    }

    [Fact]
    public async Task Handle_EmptyName_ThrowsValidationException()
    {
        // Arrange
        var command = new CreateBusinessCommand(
            Name: "",
            Niche: "tutors",
            Timezone: "UTC",
            HandoffEmail: "owner@test.com",
            WidgetGreeting: null,
            BrandColor: null);

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() => _handler.Handle(command));
    }

    [Fact]
    public async Task Handle_InvalidEmail_ThrowsValidationException()
    {
        // Arrange
        var command = new CreateBusinessCommand(
            Name: "Good Tutors",
            Niche: "tutors",
            Timezone: "UTC",
            HandoffEmail: "not-an-email",
            WidgetGreeting: null,
            BrandColor: null);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => _handler.Handle(command));
        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("valid email"));
    }

    [Fact]
    public async Task Handle_InvalidBrandColor_ThrowsValidationException()
    {
        // Arrange
        var command = new CreateBusinessCommand(
            Name: "Good Tutors",
            Niche: "tutors",
            Timezone: "UTC",
            HandoffEmail: "owner@test.com",
            WidgetGreeting: null,
            BrandColor: "red");

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationException>(() => _handler.Handle(command));
        ex.Errors.ShouldContain(e => e.ErrorMessage.Contains("hex color"));
    }

    public void Dispose() => _db.Dispose();
}
