using Buynest.Catalog.Application.Products;
using Buynest.Catalog.Domain.Products;
using Buynest.SharedKernel.Paging;

namespace Buynest.Catalog.Application.Abstractions
{
    /// <summary>Porta de saída: publicar eventos de integração (adapter = MassTransit/RabbitMQ).</summary>
    public interface IIntegrationEventPublisher
    {
        Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
    }

    /// <summary>Porta de saída: cache da listagem inicial (adapter = Redis).</summary>
    public interface IProductListCache
    {
        Task<PagedResult<ProductListItem>?> GetFirstPageAsync(Guid tenantId, CancellationToken cancellationToken = default);
        Task SetFirstPageAsync(Guid tenantId, PagedResult<ProductListItem> page, CancellationToken cancellationToken = default);
        Task InvalidateAsync(Guid tenantId, CancellationToken cancellationToken = default);
    }

    /// <summary>Consultas de leitura (lado "Q" do CQRS) - implementadas na Infra com projeções.</summary>
    public interface IProductQueries
    {
        Task<PagedResult<ProductListItem>> ListAsync(ProductFilter filter, CancellationToken cancellationToken = default);
        Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<StatusHistoryResponse>> GetStatusHistoryAsync(Guid productId, CancellationToken cancellationToken = default);
    }

    public record ProductFilter(PagedRequest Page, ProductStatus? Status, ProductCategory? Category)
    {
        public bool IsInitialList =>
            Page.SafePageIndex == 1 && Page.SafePageSize == 10 &&
            string.IsNullOrWhiteSpace(Page.Search) && Status is null && Category is null;
    }
}
