using Buynest.SharedKernel.Domain;
using Buynest.SharedKernel.Exceptions;

namespace Buynest.Inventory.Api.Domain
{
    public sealed class StockReservation : Entity
    {
        private StockReservation() {}

        public Guid StockItemId { get; private set; }
        public Guid OrderId { get; private set; }
        public int Quantity { get; private set; }
        public ReservationStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ClosedAt { get; private set; }

        internal static StockReservation Create(Guid stockItemId, Guid orderId, int quantity) => new()
        {
            StockItemId = stockItemId,
            OrderId = orderId,
            Quantity = quantity,
            Status = ReservationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        internal void Confirm() => Close(ReservationStatus.Confirmed);
        internal void Release() => Close(ReservationStatus.Released);

        private void Close(ReservationStatus status)
        {
            if (Status != ReservationStatus.Pending)
                throw new BusinessRuleException("reservation_closed", "Reserva já finalizada.");
            Status = status;
            ClosedAt = DateTime.UtcNow;
        }
    }
}