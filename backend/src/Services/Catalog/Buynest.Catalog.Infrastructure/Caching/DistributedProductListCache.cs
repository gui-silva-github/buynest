using System.Text.Json;
using Buynest.Catalog.Application.Abstractions;
using Buynest.Catalog.Application.Products;
using Buynest.SharedKernel.Paging;
using Microsoft.Extensions.Caching.Distributed;

namespace Buynest.Catalog.Infrastructure.Caching
{
    /// <summary>Cache da 1ª página (Redis via IDistributedCache). Invalidado a cada escrita.</summary>
    public sealed class DistributedProductListCache(IDistributedCache cache): IProductListCache
    {
        private static readonly DistributedCacheEntryOptions EntryOptions = new()
        {
          AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)  
        };

        private static string Key(Guid tenantId) => $"catalog:{tenantId}:products:first-page";

        public async Task<PagedResult<ProductListItem>?> GetFirstPageAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            var json = await cache.GetStringAsync(Key(tenantId), cancellationToken);
            return json is null ? null : JsonSerializer.Deserialize<PagedResult<ProductListItem>>(json);
        }

        public Task SetFirstPageAsync(Guid tenantId, PagedResult<ProductListItem> page, CancellationToken cancellationToken = default) => 
            cache.SetStringAsync(Key(tenantId), JsonSerializer.Serialize(page), EntryOptions, cancellationToken);

        public Task InvalidateAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
            cache.RemoveAsync(Key(tenantId), cancellationToken);
    }
}