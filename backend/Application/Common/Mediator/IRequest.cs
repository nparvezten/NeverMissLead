namespace NeverMissLead.Application.Common.Mediator;

/// <summary>Marker interface for all CQRS requests (commands and queries).</summary>
/// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
public interface IRequest<TResponse>;
