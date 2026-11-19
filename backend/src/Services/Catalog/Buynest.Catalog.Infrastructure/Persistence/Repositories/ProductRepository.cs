using Buynest.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Buynest.Catalog.Infrastructure.Persistence.Repositories
{
    public sealed class ProductRepository(CatalogDbContext db) : IProductRepository
    {
        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        public Task<bool> SkuExistsAsync(string sku, Guid? ignoreProductId = null, CancellationToken cancellationToken = default)
        {
            var normalized = sku.Trim().ToUpperInvariant();
            return db.Products.AnyAsync(p => p.Sku == normalized && p.Id != ignoreProductId, cancellationToken);
        }

        public void Add(Product product) => db.Products.Add(product);

        public void Remove(Product product) => db.Products.Remove(product);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
    }
}