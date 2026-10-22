namespace Buynest.Contracts {
    /// <summary>Catalog -> Inventory: produto cadastrado; criar item de estoque zerado.</summary>
    public record ProductCreated(Guid ProductId, Guid TenantId, string Sku, string Name, decimal Price);

    /// <summary>Catalog -> Inventory: produto removido.</summary>
    public record ProductDeleted(Guid ProductId, Guid TenantId);
}