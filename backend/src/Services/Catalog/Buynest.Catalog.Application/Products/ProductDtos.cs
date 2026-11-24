using Buynest.Catalog.Domain.Products;

namespace Buynest.Catalog.Application.Products
{
    // Records = DTOs imutáveis. São exatamente as propriedades que o front recebe.
    public record ProductListItem(
        Guid Id, string Sku, string Name, decimal Price,
        ProductCategory Category, ProductStatus Status, bool IsFeatured, DateTime CreatedAt
    );

    public record ProductResponse(
        Guid Id, string Sku, string Name, string? Description, decimal Price,
        ProductCategory Category, ProductStatus Status, string? StatusReason,
        bool IsFeatured, DateTime CreatedAt, DateTime? UpdatedAt
    );

    public record StatusHistoryResponse(
        Guid Id, ProductStatus? FromStatus, ProductStatus ToStatus,
        string? Reason, string? ChangedBy, DateTime ChangedAt
    );
}
