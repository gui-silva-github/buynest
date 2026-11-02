using Buynest.SharedKernel.Domain;
using Buynest.SharedKernel.Exceptions;

namespace Buynest.Inventory.Api.Domain
{
    /// <summary>
    /// Aggregate Root do estoque de UM produto
    ///     Disponível = Em estoque - Reservado
    /// Reservas seguram unidades enquanto o pagamento é processado (saga).
    /// </summary>
    public sealed class StockItem : Entity, IAggregateRoot, ITenantEntity
    {
        private readonly List<StockMovement> _movements = [];
        private readonly List<StockReservation> _reservations = [];

        private StockItem() {}

        public Guid TenantId { get; private set; }
        public Guid ProductId { get; private set; }
        public string Sku { get; private set; } = null!;
        public string ProductName { get; private set; } = null!;
        public int QuantityOnHand { get; private set; }
        public int QuantityReserved { get; private set; }
        public int MinimumQuantity { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        /// <summary>Concorrência otimista (coluna xmin do PostgreSQL): evita vender a mesma última unidade 2x.</summary>
        public uint Version { get; private set; }

        public int QuantityAvailable => QuantityOnHand - QuantityReserved;
        public bool IsBelowMinimum => QuantityAvailable <= MinimumQuantity;

        public IReadOnlyCollection<StockMovement> Movements => _movements.AsReadOnly();
        public IReadOnlyCollection<StockReservation> Reservations => _reservations.AsReadOnly();

        public static StockItem Create(Guid tenantId, Guid productId, string sku, string productName) => new()
        {
            TenantId = tenantId,
            ProductId = productId,
            Sku = sku,
            ProductName = productName,
            UpdatedAt = DateTime.UtcNow
        };

        public void RegisterEntry(int quantity, string? reason, string? user)
        {
            EnsurePositive(quantity);
            QuantityOnHand += quantity;
            AddMovement(MovementType.Entry, quantity, reason, user);
        }

        public void RegisterExit(int quantity, string? reason, string? user)
        {
            EnsurePositive(quantity);
            if (quantity > QuantityAvailable)
                throw new BusinessRuleException("insufficient_stock", $"Saída maior que o disponível ({QuantityAvailable}).");

            QuantityOnHand -= quantity;
            AddMovement(MovementType.Exit, -quantity, reason, user);
        }

        public void Adjust(int newQuantityOnHand, string? reason, string? user)
        {
            if (newQuantityOnHand < QuantityReserved)
                throw new BusinessRuleException("adjust_below_reserved", 
                    $"O saldo não pode ficar abaixo do reservado ({QuantityReserved}).");

            var delta = newQuantityOnHand - QuantityOnHand;
            QuantityOnHand = newQuantityOnHand;
            AddMovement(MovementType.Adjustment, delta, reason ?? "Ajuste de inventário", user);
        }

        public void SetMinimumQuantity(int minimum)
        {
            if (minimum < 0)
                throw new BusinessRuleException("invalid_minimum", "Estoque mínimo não pode ser negativo.");
            MinimumQuantity = minimum;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool CanReserve(int quantity) => quantity > 0 && quantity <= QuantityAvailable;

        public void Reserve(Guid orderId, int quantity)
        {
            if (!CanReserve(quantity))
                throw new BusinessRuleException("insufficient_stock",
                    $"Estoque insuficiente para {ProductName}: disponível {QuantityAvailable}, solicitado {quantity}.");
            
            QuantityReserved += quantity;
            _reservations.Add(StockReservation.Create(Id, orderId, quantity));
            AddMovement(MovementType.Reservation, 0, $"Reserva de {quantity} un.", null, orderId);
        }

        /// <summary>Compensação da saga: pagamento recusado -> devolve as unidades ao disponível.</summary>
        public void ReleaseReservation(Guid orderId)
        {
            foreach (var reservation in PendingReservationsOf(orderId))
            {
                reservation.Release();
                QuantityReserved -= reservation.Quantity;
                AddMovement(MovementType.ReservationRelease, 0, $"Reserva liberada ({reservation.Quantity} un.)", null, orderId);
            }
        }

        /// <summary>Pagamento aprovado -> baixa definitiva.</summary>
        public void ConfirmReservation(Guid orderId)
        {
            foreach (var reservation in PendingReservationsOf(orderId))
            {
                reservation.Confirm();
                QuantityReserved -= reservation.Quantity;
                QuantityOnHand -= reservation.Quantity;
                AddMovement(MovementType.Sale, -reservation.Quantity, "Venda confirmada", null, orderId);
            }
        }

        public bool HasReservationFor(Guid orderId) => _reservations.Any(r => r.OrderId == orderId);

        private List<StockReservation> PendingReservationsOf(Guid orderId) =>
            _reservations.Where(r => r.OrderId == orderId && r.Status == ReservationStatus.Pending).ToList();

        private void AddMovement(MovementType type, int delta, string? reason, string? user, Guid? orderId = null)
        {
            UpdatedAt = DateTime.UtcNow;
            _movements.Add(StockMovement.Create(Id, type, delta, QuantityOnHand, reason, user, orderId));
        }

        private static void EnsurePositive(int quantity)
        {
            if (quantity <= 0)
                throw new BusinessRuleException("invalid_quantity", "A quantidade deve ser maior que zero.");
        }
    }
}