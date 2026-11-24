namespace Buynest.Catalog.Domain.Products
{
    /// <summary>
    /// Porta de persistência do agregado. A interface fica no Domínio; a implementação (EF) na Infra.
    /// (Inversão de Dependência - o "D" do SOLID)
    /// </summary>
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<bool> SkuExistsAsync(string sku, Guid? ignoreProductId = null, CancellationToken cancellationToken = default);
        void Add(Product product);
        void Remove(Product product);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
