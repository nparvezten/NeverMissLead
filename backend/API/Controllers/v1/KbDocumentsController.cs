using Microsoft.AspNetCore.Mvc;
using NeverMissLead.Application.Common.Mediator;
using NeverMissLead.Application.KbDocuments.Commands.EmbedDocument;

namespace NeverMissLead.API.Controllers.v1;

/// <summary>
/// Knowledge-base document management for a business.
/// Owner-only endpoints (auth will be enforced in a later sprint).
/// </summary>
[ApiController]
[Route("api/v1/businesses/{businessId:guid}/documents")]
public sealed class KbDocumentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public KbDocumentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Uploads and embeds a new knowledge document for the business.
    /// The raw text is chunked, embedded, and written to <c>kb_chunks</c>
    /// by the Python RAG service.
    /// </summary>
    /// <response code="201">Document created and embedded.</response>
    /// <response code="422">Validation failed.</response>
    [HttpPost]
    [ProducesResponseType(typeof(EmbedDocumentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Embed(
        Guid businessId,
        [FromBody] EmbedDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new EmbedDocumentCommand(businessId, request.Title, request.RawText);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(Embed), new { businessId }, result);
    }
}

/// <summary>Request body for uploading a knowledge document.</summary>
public sealed record EmbedDocumentRequest(string Title, string RawText);
