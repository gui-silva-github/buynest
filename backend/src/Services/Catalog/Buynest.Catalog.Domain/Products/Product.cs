using Buynest.SharedKernel.Domain;
using Buynest.SharedKernel.Exceptions;

namespace Buynest.Catalog.Domain.Products
{
    /// <summary>
    /// Aggregate Root "Produto". Toda mudança de estado passa por métodos com regra de negócio
    /// (nada de "setters" públicos): o objeto nunca fica inválido.
    /// </summary>
    public sealed class Product : Entity, IAggregateRoot, ITenantEntity
    {
        private readonly List<ProductStatusHistory> _statusHistory = [];

        private Product() {} // EF Core

        public Guid TenantId { get; private set; }
        public string Sku { get; private set; } = null!;
        public string Name { get; private set; } = null!;
        public string? Description { get; private set; }
        public decimal Price { get; private set; }
        public ProductCategory Category { get; private set; }
        public bool IsFeatured { get; private set; }
        public ProductStatus Status { get; private set; }
        public string? StatusReason { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public IReadOnlyCollection<ProductStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

        public bool IsAvailableForSale => Status == ProductStatus.Active;

        // Factory Method (padrão criacional)
        public static Product Create(
            Guid tenantId, string sku, string name, string? description,
            decimal price, ProductCategory category, bool isFeatured, string? createdBy
        )
        {
            EnsureValidPrice(price);

            var product = new Product
            {
                TenantId = tenantId,
                Sku = sku.Trim().ToUpperInvariant(),
                Name = name.Trim(),
                Description = description?.Trim(),
                Price = price,
                Category = category,
                IsFeatured = isFeatured,
                Status = ProductStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            product._statusHistory.Add(ProductStatusHistory.Create(product.Id, null, ProductStatus.Active, "Cadastro", createdBy));
            return product;    
        }

        public void Update(string name, string? description, decimal price, ProductCategory category, bool isFeatured)
        {
            if (Status == ProductStatus.Blocked)
                throw new BusinessRuleException("product_blocked", "Produto bloqueado não pode ser editado. Desbloqueie primeiro.");

            EnsureValidPrice(price);

            Name = name.Trim();
            Description = description?.Trim();
            Price = price;
            Category = category;
            IsFeatured = isFeatured;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>Usado pelo "Modal de Bloqueio / Alteração de Status". Gera histórico.</summary>
        public void ChangeStatus(ProductStatus newStatus, string? reason, string? changedBy)
        {
            if (newStatus == Status)
                throw new BusinessRuleException("status_unchanged", "O produto já está neste status.");
            
            if (newStatus == ProductStatus.Blocked && string.IsNullOrWhiteSpace(reason))
                throw new BusinessRuleException("block_reason_required", "Informe o motivo do bloqueio.");

            _statusHistory.Add(ProductStatusHistory.Create(Id, Status, newStatus, reason, changedBy));

            Status = newStatus;
            StatusReason = reason?.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        private static void EnsureValidPrice(decimal price)
        {
            if (price <= 0)
                throw new BusinessRuleException("invalid_price", "O preço deve ser maior que zero.");
        }
    }
}
