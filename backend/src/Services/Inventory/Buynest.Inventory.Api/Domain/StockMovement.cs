using Buynest.SharedKernel.Domain;

namespace Buynest.Inventory.Api.Domain
{
    /// <summary>Histórico imutável (append-only) de tudo que aconteceu com o saldo.</summary>
    public sealed class StockMovement : Entity
    {
        private StockMovement() {}

        public Guid StockItemId { get; private set; }
        public MovementType Type { get; private set; }
        public int QuantityDelta { get; private set; }
        public int BalanceAfter { get; private set; }
        public string? Reason { get; private set; }
        public string? CreatedBy { get; private set; }
        public Guid? OrderId { get; private set; }
        public DateTime CreatedAt { get; private set; }

        internal static StockMovement Create(
            Guid stockItemId, MovementType type, int delta, int balanceAfter,
            string? reason, string? createdBy, Guid? orderId
        ) => new()
        {
           StockItemId = stockItemId,
            Type = type,
            QuantityDelta = delta,
            BalanceAfter = balanceAfter,
            Reason = reason,
            CreatedBy = createdBy,
            OrderId = orderId,
            CreatedAt = DateTime.UtcNow 
        };
    }
}