using Buynest.Catalog.Application.Abstractions;
using Buynest.Catalog.Domain.Products;
using Buynest.Catalog.Infrastructure.Caching;
using Buynest.Catalog.Infrastructure.Messaging;
using Buynest.Catalog.Infrastructure.Persistence;
using Buynest.Catalog.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Buynest.Catalog.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<CatalogDbContext>(o => o.UseNpgsql(configuration.GetConnectionString("Database")));

            services.AddStackExchangeRedisCache(o => o.Configuration = configuration.GetConnectionString("Redis"));

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IProductListCache, DistributedProductListCache>();
            services.AddScoped<IIntegrationEventPublisher, MassTransitEventPublisher>();

            return services;
        }
    }
}
