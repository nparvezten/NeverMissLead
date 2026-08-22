using NeverMissLead.Application.Common.Mediator;

namespace NeverMissLead.Application.Auth.Commands.Login;

public sealed record LoginResponse(
    string Token,
    Guid BusinessId,
    string BusinessName,
    string Email);

public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<LoginResponse>;
