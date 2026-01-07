using MyApp.Domain.Events;

namespace MyApp.Application.Abstractions
{
    public interface IMessageBus
    {
        Task PublishAsync<TEvent>(TEvent evt, CancellationToken ct = default) where TEvent : IDomainEvent;
    }
}