namespace Buynest.Contracts {
    public record OrderLine(Guid ProductId, int Quantity, decimal UnitPrice);

    /// <summary>Ordering -> Saga: pedido criado (início da saga).</summary>
    public record OrderSubmitted(
        Guid OrderId,
        Guid TenantId,
        Guid CustomerId,
        decimal Total,
        string PaymentMethod,
        IReadOnlyList<OrderLine> Lines,
        string? PaymentToken = null
    );

    /// <summary>Saga -> Ordering: pedido concluído com sucesso.</summary>
    public record OrderConfirmed(Guid OrderId);

    /// <summary>Saga -> Ordering: pedido cancelado (compensação).</summary>
    public record OrderCancelled(Guid OrderId, string Reason); 
}