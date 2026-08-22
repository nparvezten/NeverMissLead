using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.Conversations.Queries.GetConversationDetails;
using NeverMissLead.Application.Conversations.Queries.GetConversations;
using NeverMissLead.Application.Conversations.Queries.GetUnansweredQuestions;
using NeverMissLead.Application.FollowUps.Queries.GetFollowUpTasks;
using NeverMissLead.Application.Leads.Commands.UpdateLeadStatus;
using NeverMissLead.Application.Leads.Queries.GetLeads;
using NeverMissLead.Domain.Enums;

namespace NeverMissLead.API.Controllers.v1;

[Authorize]
[ApiController]
[Route("api/v1/dashboard")]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    private Guid GetAuthenticatedBusinessId()
    {
        var businessIdStr = User.FindFirst("business_id")?.Value;
        if (string.IsNullOrEmpty(businessIdStr) || !Guid.TryParse(businessIdStr, out var businessId))
        {
            throw new UnauthorizedAccessException("Missing or invalid business identifier in authentication claims.");
        }
        return businessId;
    }

    /// <summary>
    /// Gets all leads for the authenticated business with decrypted contact info.
    /// </summary>
    [HttpGet("leads")]
    [ProducesResponseType(typeof(IReadOnlyList<LeadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeads(CancellationToken cancellationToken)
    {
        var businessId = GetAuthenticatedBusinessId();
        var leads = await _mediator.Send(new GetLeadsQuery(businessId), cancellationToken);
        return Ok(leads);
    }

    public sealed record UpdateStatusRequest(LeadStatus Status);

    /// <summary>
    /// Updates the qualification lifecycle status of a lead.
    /// </summary>
    [HttpPatch("leads/{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLeadStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var businessId = GetAuthenticatedBusinessId();
        await _mediator.Send(new UpdateLeadStatusCommand(id, businessId, request.Status), cancellationToken);
        return Ok(new { message = $"Lead status updated to {request.Status}." });
    }

    /// <summary>
    /// Gets a list of conversations for the authenticated business.
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(IReadOnlyList<ConversationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        var businessId = GetAuthenticatedBusinessId();
        var conversations = await _mediator.Send(new GetConversationsQuery(businessId), cancellationToken);
        return Ok(conversations);
    }

    /// <summary>
    /// Gets full thread and cited KB chunk details for a specific conversation.
    /// </summary>
    [HttpGet("conversations/{id:guid}")]
    [ProducesResponseType(typeof(ConversationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversationDetails(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var businessId = GetAuthenticatedBusinessId();
        var conversation = await _mediator.Send(new GetConversationDetailsQuery(id, businessId), cancellationToken);
        if (conversation is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Conversation Not Found",
                Detail = $"Conversation '{id}' was not found for this business."
            });
        }
        return Ok(conversation);
    }

    /// <summary>
    /// Gets conversations where the AI abstained (NeedsHuman = true), highlighting FAQ gaps.
    /// </summary>
    [HttpGet("unanswered-questions")]
    [ProducesResponseType(typeof(IReadOnlyList<UnansweredQuestionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnansweredQuestions(CancellationToken cancellationToken)
    {
        var businessId = GetAuthenticatedBusinessId();
        var unanswered = await _mediator.Send(new GetUnansweredQuestionsQuery(businessId), cancellationToken);
        return Ok(unanswered);
    }

    /// <summary>
    /// Gets all pending and sent follow-up tasks for the business.
    /// </summary>
    [HttpGet("follow-ups")]
    [ProducesResponseType(typeof(IReadOnlyList<FollowUpTaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFollowUpTasks(CancellationToken cancellationToken)
    {
        var businessId = GetAuthenticatedBusinessId();
        var tasks = await _mediator.Send(new GetFollowUpTasksQuery(businessId), cancellationToken);
        return Ok(tasks);
    }
}
