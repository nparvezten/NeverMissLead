namespace NeverMissLead.Application.Common.Mediator;

/// <summary>
/// Handles a CQRS request and returns a response.
/// Implementations are discovered and registered by assembly scanning at startup.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Processes the request and returns the response.</summary>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
}
