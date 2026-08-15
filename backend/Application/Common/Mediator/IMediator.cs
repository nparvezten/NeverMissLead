namespace NeverMissLead.Application.Common.Mediator;

/// <summary>Dispatches a request to its registered handler.</summary>
public interface IMediator
{
    /// <summary>Sends a request to its handler and returns the response.</summary>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
