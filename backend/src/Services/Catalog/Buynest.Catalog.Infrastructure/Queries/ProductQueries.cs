using Buynest.BuildingBlocks.Persistence;
using Buynest.Catalog.Application.Abstractions;
using Buynest.Catalog.Application.Products;
using Buynest.Catalog.Infrastructure.Persistence;
using Buynest.SharedKernel.Paging;
using Microsoft.EntityFrameworkCore;

namespace Buynest.Catalog.Infrastructure.Queries {
    /// <summary>Leitura otimizada: AsNoTracking + projeção direto para DTO (Select).</summary>
    public sealed class ProductQueries(CatalogDbContext db) : IProductQueries
    {
        public Task<PagedResult<ProductListItem>> ListAsync(ProductFilter filter, CancellationToken cancellationToken = default)
        {
            var query = db.Products.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Page.Search))
            {
                var term = $"%{filter.Page.Search.Trim()}%";
                query = query.Where(p => EF.Functions.ILike(p.Name, term) || EF.Functions.ILike(p.Sku, term));
            }

            if (filter.Status is not null) query = query.Where(p => p.Status == filter.Status);
            if (filter.Category is not null) query = query.Where(p => p.Category == filter.Category);

            return query
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductListItem(p.Id, p.Sku, p.Name, p.Price, p.Category, p.Status, p.IsFeatured, p.CreatedAt))
                .ToPagedResultAsync(filter.Page, cancellationToken);
        }

        public Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            db.Products.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductResponse(
                    p.Id, p.Sku, p.Name, p.Description, p.Price, p.Category, p.Status,
                    p.StatusReason, p.IsFeatured, p.CreatedAt, p.UpdatedAt
                ))
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<IReadOnlyList<StatusHistoryResponse>> GetStatusHistoryAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            // garante que o produto é do tenant atual (o filtro global está em Products)
            var exists = await db.Products.AnyAsync(p => p.Id == productId, cancellationToken);
            if (!exists) return [];

            return await db.ProductStatusHistory.AsNoTracking()
                .Where(h => h.ProductId == productId)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new StatusHistoryResponse(h.Id, h.FromStatus, h.ToStatus, h.Reason, h.ChangedBy, h.ChangedAt))
                .ToListAsync(cancellationToken);
        } 
    }
}
