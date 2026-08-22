using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IValidator<LoginCommand> _validator;

    public LoginCommandHandler(
        IApplicationDbContext db,
        IJwtTokenService jwtTokenService,
        IPasswordHashService passwordHashService,
        IValidator<LoginCommand> validator)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _passwordHashService = passwordHashService;
        _validator = validator;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        // Find business by registered owner/handoff email
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var settings = await _db.BusinessSettings
            .Include(s => s.Business)
            .FirstOrDefaultAsync(
                s => s.HandoffEmail.ToLower() == normalizedEmail,
                cancellationToken);

        if (settings is null || settings.Business is null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Verify cryptographic PBKDF2 password hash
        if (string.IsNullOrEmpty(settings.PasswordHash) ||
            !_passwordHashService.VerifyPassword(request.Password, settings.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var token = _jwtTokenService.GenerateToken(
            settings.BusinessId,
            settings.HandoffEmail,
            settings.Business.Name);

        return new LoginResponse(
            token,
            settings.BusinessId,
            settings.Business.Name,
            settings.HandoffEmail);
    }
}
