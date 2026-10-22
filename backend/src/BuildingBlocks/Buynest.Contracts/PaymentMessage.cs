namespace Buynest.Contracts {
    public static class PaymentMethods {
        public const string CreditCard = "CreditCard";
        public const string Pix = "Pix";
    }

    /// <summary>
    /// Saga -> Payment: cobrar pedido.
    /// PaymentToken = referência do meio de pagamento já tokenizado pelo provedor
    /// (no Stripe: um PaymentMethod "pm_...") O número do cartão NUNCA passa pelo backend.
    /// </summary>
    public record RequestPayment(Guid OrderId, Guid TenantId, decimal Amount, string Method, string? PaymentToken = null);

    /// <summary>Payment -> Saga</summary>
    public record PaymentApproved(Guid OrderId, Guid PaymentId);

    /// <summary>Payment -> Saga.</summary>
    public record PaymentRejected(Guid OrderId, Guid PaymentId, string Reason);
}