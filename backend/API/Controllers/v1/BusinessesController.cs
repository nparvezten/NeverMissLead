using Microsoft.AspNetCore.Mvc;
using NeverMissLead.Application.Businesses.Commands.CreateBusiness;
using NeverMissLead.Application.Businesses.Queries.GetBusiness;
using NeverMissLead.Application.Common.Mediator;

namespace NeverMissLead.API.Controllers.v1;

/// <summary>
/// Business CRUD endpoints.
/// All dashboard endpoints will require auth in a future sprint;
/// these are unguarded for Week 1 scaffolding.
/// </summary>
[ApiController]
[Route("api/v1/businesses")]
public sealed class BusinessesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BusinessesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Creates a new business and returns its ID.</summary>
    /// <response code="201">Business created successfully.</response>
    /// <response code="422">Validation failed.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBusinessRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateBusinessCommand(
            request.Name,
            request.Niche,
            request.Timezone ?? "UTC",
            request.HandoffEmail,
            request.WidgetGreeting,
            request.BrandColor);

        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Fetches a business by ID.</summary>
    /// <response code="200">Business found.</response>
    /// <response code="404">Business not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BusinessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(new GetBusinessQuery(id), cancellationToken);

        return dto is not null
            ? Ok(dto)
            : NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Not Found",
                Detail = $"Business '{id}' was not found.",
                Instance = HttpContext.Request.Path
            });
    }
}

/// <summary>Request body for creating a business.</summary>
public sealed record CreateBusinessRequest(
    string Name,
    string Niche,
    string HandoffEmail,
    string? Timezone,
    string? WidgetGreeting,
    string? BrandColor);
