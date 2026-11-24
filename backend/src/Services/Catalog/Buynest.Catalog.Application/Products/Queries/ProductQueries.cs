using Buynest.Catalog.Application.Abstractions;
using Buynest.SharedKernel.Exceptions;
using Buynest.SharedKernel.Paging;
using Buynest.SharedKernel.Tenancy;
using MediatR;

namespace Buynest.Catalog.Application.Products.Queries
{
    // QUERIES: só leem estado (lado "Q" do CQRS)
    
    public record ListProductsQuery(ProductFilter Filter) : IRequest<PagedResult<ProductListItem>>;

    public sealed class ListProductsHandler(IProductQueries queries, IProductListCache cache, ITenantContext tenant) : IRequestHandler<ListProductsQuery, PagedResult<ProductListItem>>
    {
        public async Task<PagedResult<ProductListItem>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
        {
            // "Cache client para a listagem inicial": 1ª página sem filtro vem do Redis
            if (!request.Filter.IsInitialList)
                return await queries.ListAsync(request.Filter, cancellationToken);

            var cached = await cache.GetFirstPageAsync(tenant.TenantId, cancellationToken);
            if (cached is not null) return cached;

            var page = await queries.ListAsync(request.Filter, cancellationToken);
            await cache.SetFirstPageAsync(tenant.TenantId, page, cancellationToken);
            return page;
        }
    }

    public record GetProductByIdQuery(Guid Id) : IRequest<ProductResponse>;

    public sealed class GetProductByIdHandler(IProductQueries queries) : IRequestHandler<GetProductByIdQuery, ProductResponse>
    {
        public async Task<ProductResponse> Handle(GetProductByIdQuery request, CancellationToken cancellationToken) =>
            await queries.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Produto", request.Id);
    }

    public record GetProductStatusHistoryQuery(Guid ProductId) : IRequest<IReadOnlyList<StatusHistoryResponse>>;

    public sealed class GetProductStatusHistoryHandler(IProductQueries queries) : IRequestHandler<GetProductStatusHistoryQuery, IReadOnlyList<StatusHistoryResponse>>
    {
        public Task<IReadOnlyList<StatusHistoryResponse>> Handle(GetProductStatusHistoryQuery request, CancellationToken cancellationToken) =>
            queries.GetStatusHistoryAsync(request.ProductId, cancellationToken); 
    }
}
