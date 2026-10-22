namespace Buynest.Contracts {
    // Comandos (imperativo) enviados pela saga
    public record ReserveStock(Guid OrderId, Guid TenantId, IReadOnlyList<OrderLine> Lines);
    public record ConfirmStock(Guid OrderId);
    public record ReleaseStock(Guid OrderId);

    // Eventos (passado) publicados pelo Inventory
    public record StockReserved(Guid OrderId);
    public record StockReservationFailed(Guid OrderId, string Reason);
}