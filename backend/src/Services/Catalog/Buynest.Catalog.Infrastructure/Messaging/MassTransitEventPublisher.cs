using Buynest.Catalog.Application.Abstractions;
using MassTransit;

namespace Buynest.Catalog.Infrastructure.Messaging
{
    ///<summary>Adapter: a Aplicação não sabe que existe MassTransit/RabbitMQ (padrão Adapter).</summary>
    public sealed class MassTransitEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
    {
        public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class =>
            publishEndpoint.Publish(message, cancellationToken);
    }
}