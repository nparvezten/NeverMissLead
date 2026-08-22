using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Auth.Commands.Login;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Domain.Entities;
using NeverMissLead.Infrastructure.Persistence;
using NeverMissLead.Infrastructure.Security;
using NSubstitute;
using Shouldly;
using Xunit;

namespace NeverMissLead.Tests.Auth;

public sealed class LoginCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IValidator<LoginCommand> _validator;
    private readonly Guid _businessId;

    public LoginCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _passwordHashService = new PasswordHashService();
        _validator = new LoginCommandValidator();

        var business = Business.Create("Bright Minds Coaching", "private tutors");
        _businessId = business.Id;
        
        // Hash password with PBKDF2
        var hashedPassword = _passwordHashService.HashPassword("BrightMinds2026!");
        var settings = BusinessSettings.Create(
            business.Id,
            "owner@brightminds.test",
            passwordHash: hashedPassword);

        _db.Businesses.Add(business);
        _db.BusinessSettings.Add(settings);
        _db.SaveChanges();

        _jwtTokenService.GenerateToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>())
            .Returns("MOCK_JWT_TOKEN_XYZ");
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenAndBusinessInfo()
    {
        // Arrange
        var handler = new LoginCommandHandler(_db, _jwtTokenService, _passwordHashService, _validator);
        var command = new LoginCommand("owner@brightminds.test", "BrightMinds2026!");

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.Token.ShouldBe("MOCK_JWT_TOKEN_XYZ");
        response.BusinessId.ShouldBe(_businessId);
        response.BusinessName.ShouldBe("Bright Minds Coaching");
        response.Email.ShouldBe("owner@brightminds.test");
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorizedAccess()
    {
        // Arrange
        var handler = new LoginCommandHandler(_db, _jwtTokenService, _passwordHashService, _validator);
        var command = new LoginCommand("owner@brightminds.test", "WrongPassword123!");

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Database_StoredPasswordHash_IsNotPlaintext()
    {
        // Act
        var settings = await _db.BusinessSettings.FirstAsync(s => s.BusinessId == _businessId);

        // Assert
        settings.PasswordHash.ShouldNotBeNull();
        settings.PasswordHash.ShouldNotContain("BrightMinds2026!");
        settings.PasswordHash.ShouldStartWith("100000."); // PBKDF2 iteration prefix
    }

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsUnauthorizedAccess()
    {
        // Arrange
        var handler = new LoginCommandHandler(_db, _jwtTokenService, _passwordHashService, _validator);
        var command = new LoginCommand("unknown@random.test", "Password123!");

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidEmailFormat_ThrowsValidationException()
    {
        // Arrange
        var handler = new LoginCommandHandler(_db, _jwtTokenService, _passwordHashService, _validator);
        var command = new LoginCommand("invalid-email", "Password123!");

        // Act & Assert
        await Should.ThrowAsync<ValidationException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    public void Dispose() => _db.Dispose();
}
