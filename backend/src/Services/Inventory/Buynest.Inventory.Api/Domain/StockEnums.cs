namespace Buynest.Inventory.Api.Domain
{
    public enum MovementType
    {
        Entry = 1, // entrada (compra/recebimento)
        Exit = 2, // saída manual (perda/avaria)
        Adjustment = 3, // inventário/ajuste de saldo
        Reservation = 4, // reservado para um pedido (não muda o saldo físico)
        ReservationRelease = 5, // reserva desfeita (pagamento recusado)
        Sale = 6 // baixa definitiva por venda
    }

    public enum ReservationStatus
    {
        Pending = 1,
        Confirmed = 2,
        Released = 3
    }
}