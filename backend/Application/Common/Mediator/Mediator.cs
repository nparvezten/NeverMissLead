using Microsoft.Extensions.DependencyInjection;

namespace NeverMissLead.Application.Common.Mediator;

/// <summary>
/// Lightweight native mediator — resolves handlers from the DI container.
/// Handlers are registered via assembly scanning in
/// <c>ApplicationServiceExtensions.AddApplication()</c>.
/// No MediatR; 100% MIT-licensed pattern.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _services;

    public Mediator(IServiceProvider services) => _services = services;

    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = _services.GetRequiredService(handlerType);

        // Invoke Handle via reflection — one allocation per call, acceptable at this scale.
        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle))!;
        return (Task<TResponse>)method.Invoke(handler, [request, cancellationToken])!;
    }
}
