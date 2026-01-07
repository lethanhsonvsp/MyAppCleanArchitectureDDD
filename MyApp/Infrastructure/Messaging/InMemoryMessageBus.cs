using MediatR;
using MyApp.Application.Abstractions;
using MyApp.Application.Charging;
using MyApp.Domain.Events;

namespace MyApp.Infrastructure.Messaging;

/// <summary>
/// Simple in-memory message bus for domain events
/// Uses MediatR to dispatch events to handlers
/// ✅ SINGLETON: Create scope internally to avoid lifetime conflicts
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
        // ✅ Create a new scope to safely resolve scoped services
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await mediator.Publish(evt, ct);
        }
        catch (Exception ex)
        {
            // Log but don't throw - domain events should not break main flow
            Console.WriteLine($"⚠️ Error publishing event {evt.GetType().Name}: {ex.Message}");
        }
    }
}