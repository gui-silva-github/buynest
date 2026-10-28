using MassTransmit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Buynest.BuildingBlocks.Messaging {
    public sealed class RabbitMqOptions {
        public const string Section = "RabbitMq";
        public string Host { get; init; } = "localhost";
        public string VirtualHost { get; init; } = "/";
        public string Username { get; init; } = "guest";
        public string Password { get; init; } = "guest";
    }

    public static class MessagingExtensions {
        /// <summary>
        /// Configura MassTransmit + RabbitMQ com:
        /// - Transactional Outbox (publicar evento e salvar no banco na MESMA transação)
        /// - Inbox/idempotência nos consumers (mensagem repetida não é processada duas vezes)
        /// - Retry com intervalos (falhas transitórias)
        /// </summary>
        public static IServiceCollection AddMessaging<TDbContext>(
            this IServiceCollection services,
            IConfiguration configuration,
            string serviceName,
            Action<IBusRegistrationConfigurator>? configure = null) 
            where TDbContext : DbContext
        {
            var rabbit = configuration.GetSection(RabbitMqOptions.Section).Get<RabbitMqOptions>() ?? new RabbitMqOptions();

            services.AddMassTransmit(bus => 
            {
                // filas com prefixo do serviço
                bus.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(serviceName, false));

                bus.AddEntityFrameworkOutbox<TDbContext>(outbox => 
                {
                    outbox.UsePostgres();
                    outbox.UseBusOutbox();
                    outbox.QueryDelay = TimeSpan.FromSeconds(1);
                });

                bus.AddConfigureEndpointsCallback((context, _, endpoint) =>
                {
                    endpoint.UseMessageRetry(retry => retry.Intervals(200, 1000, 3000));
                    endpoint.UseEntityFrameworkOutbox<TDbContext>(context);
                });

                configure?.Invoke(bus);

                bus.UsingRabbitMq((context, cfg) => 
                {
                    cfg.Host(rabbit.Host, rabbit.VirtualHost, host => {
                        host.Username(rabbit.Username);
                        host.Password(rabbit.Password);
                    });
                    cfg.ConfigureEndpoints(context);
                });
            });

            return services;
        }

        /// <summary>Tabelas de Inbox/Outbox do MassTransit no modelo do EF.</summary>
        public static ModelBuilder AddMessagingTables(this ModelBuilder modelBuilder)
        {
            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();
            return modelBuilder;
        }
    }
}