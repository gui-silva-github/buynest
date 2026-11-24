using Buynest.SharedKernel.Domain;

namespace Buynest.Catalog.Domain.Products
{
    /// <summary>Linha do "Modal de Histórico": quem mudou o status, de/para, quando e por quê.</summary>
    public sealed class ProductStatusHistory : Entity
    {
        private ProductStatusHistory() {}

        public Guid ProductId { get; private set; }
        public ProductStatus? FromStatus { get; private set; }
        public ProductStatus ToStatus { get; private set; }
        public string? Reason { get; private set; }
        public string? ChangedBy { get; private set; }
        public DateTime ChangedAt { get; private set; }

        internal static ProductStatusHistory Create(
            Guid productId, ProductStatus? from, ProductStatus to, string? reason, string? changedBy
        ) => new()
        {
            ProductId = productId,
            FromStatus = from,
            ToStatus = to,
            Reason = reason?.Trim(),
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow
        };
    }
}
