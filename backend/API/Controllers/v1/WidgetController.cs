using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Conversations.Commands.SendMessage;
using NeverMissLead.Application.Interfaces;
using NeverMissLead.Application.Leads.Commands.ContactCapture;

namespace NeverMissLead.API.Controllers.v1;

/// <summary>
/// Public, internet-facing endpoints for the embeddable chat widget.
/// Unauthenticated by design; strictly rate-limited and business-scoped.
/// </summary>
[ApiController]
[Route("api/v1/widget/{businessId:guid}")]
[EnableRateLimiting("widget")]
public sealed class WidgetController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _dbContext;

    public WidgetController(IMediator mediator, IApplicationDbContext dbContext)
    {
        _mediator = mediator;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Processes a visitor chat turn: retrieves grounded KB chunks, generates an answer,
    /// cites sources, and detects lead intent.
    /// </summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Chat(
        [FromRoute] Guid businessId,
        [FromBody] WidgetChatRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SendMessageCommand(
            businessId,
            request.ConversationId,
            request.Message,
            request.VisitorRef);

        var response = await _mediator.Send(command, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Captures lead contact details (name, email, phone) inline from the chat widget.
    /// </summary>
    [HttpPost("contact")]
    [ProducesResponseType(typeof(ContactCaptureResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CaptureContact(
        [FromRoute] Guid businessId,
        [FromBody] WidgetContactRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ContactCaptureCommand(
            businessId,
            request.ConversationId,
            request.Name,
            request.Phone,
            request.Email,
            request.IntentSummary);

        var leadId = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new ContactCaptureResponse(leadId));
    }

    /// <summary>
    /// Returns public widget configuration (greeting, brand color) for the business.
    /// </summary>
    [HttpGet("config")]
    [ProducesResponseType(typeof(WidgetConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfig(
        [FromRoute] Guid businessId,
        CancellationToken cancellationToken)
    {
        var business = await _dbContext.Businesses
            .Include(b => b.Settings)
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

        if (business == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = $"Business '{businessId}' was not found.",
                Instance = HttpContext.Request.Path
            });
        }

        var greeting = business.Settings?.WidgetGreeting ?? "Hi! How can I help you today?";
        var brandColor = business.Settings?.BrandColor ?? "#6366f1";

        return Ok(new WidgetConfigResponse(business.Id, business.Name, greeting, brandColor));
    }
}

public sealed record WidgetChatRequest(
    Guid? ConversationId,
    string Message,
    string? VisitorRef);

public sealed record WidgetContactRequest(
    Guid ConversationId,
    string Name,
    string? Phone,
    string? Email,
    string? IntentSummary);

public sealed record ContactCaptureResponse(Guid LeadId);

public sealed record WidgetConfigResponse(
    Guid BusinessId,
    string BusinessName,
    string WidgetGreeting,
    string BrandColor);
