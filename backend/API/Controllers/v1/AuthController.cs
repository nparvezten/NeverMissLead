using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NeverMissLead.Application.Auth.Commands.Login;
using NeverMissLead.Application.Common.Mediator;

namespace NeverMissLead.API.Controllers.v1;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public sealed record LoginRequest(string Email, string Password);
    public sealed record AuthUserResponse(Guid BusinessId, string BusinessName, string Email);

    /// <summary>
    /// Authenticates a business owner and sets an HttpOnly JWT session cookie.
    /// Rate-limited to prevent brute-force attacks.
    /// </summary>
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var response = await _mediator.Send(command, cancellationToken);

        // Set HttpOnly, SameSite=Strict secure cookie
        Response.Cookies.Append("nml_token", response.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/"
        });

        return Ok(new AuthUserResponse(response.BusinessId, response.BusinessName, response.Email));
    }

    /// <summary>
    /// Logs out the business owner by clearing the auth session cookie.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("nml_token", new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/"
        });

        return Ok(new { message = "Logged out successfully." });
    }

    /// <summary>
    /// Returns the currently authenticated owner profile from claims.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(AuthUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var businessIdStr = User.FindFirst("business_id")?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        var businessName = User.FindFirst("business_name")?.Value ?? string.Empty;

        if (string.IsNullOrEmpty(businessIdStr) || !Guid.TryParse(businessIdStr, out var businessId))
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Authentication token does not contain a valid business identifier."
            });
        }

        return Ok(new AuthUserResponse(businessId, businessName, email));
    }
}
