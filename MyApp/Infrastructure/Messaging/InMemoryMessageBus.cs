using MediatR;
using MyApp.Application.Abstractions;
using MyApp.Application.Charging;
using MyApp.Domain.Events;

namespace MyApp.Infrastructure.Messaging;

/// <summary>
/// Simple in-memory message bus for domain events
/// Uses MediatR to dispatch events to handlers
/// </summary>
public class InMemoryMessageBus : IMessageBus
{
    private readonly IServiceProvider _serviceProvider;

    public InMemoryMessageBus(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<TEvent>(TEvent evt, CancellationToken ct = default)
        where TEvent : IDomainEvent
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Publish(evt, ct);
    }
}