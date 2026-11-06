using Buynest.SharedKernel.Domain;
using Buynest.SharedKernel.Exceptions;

namespace Buynest.Payment.Api.Domain
{
    public enum PaymentStatus
    {
        Pending = 1,
        Approved = 2,
        Rejected = 3
    }   

    /// <summary> Máquina de estados simples: Pending -> Approved | Rejected (padrão State).</summary>
    public sealed class Payment : Entity, IAggregateRoot, ITenantEntity
    {
        private Payment() {}

        public Guid TenantId { get; private set; }
        public Guid OrderId { get; private set; }
        public decimal Amount { get; private set; }
        public string Method { get; private set; } = null!;
        /// <summary>Quem processou (hoje sempre "Stripe"; a coluna permite trocar/somar provedores).</summary>
        public string Provider { get; private set; } = null!;
        public PaymentStatus Status { get; private set; }
        public string? ProviderReference { get; private set; }
        public string? FailureReason { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        public static Payment Create(Guid tenantId, Guid orderId, decimal amount, string method, string provider)
        {
            if (amount <= 0) throw new BusinessRuleException("invalid_amount", "Valor inválido.");

            return new Payment
            {
                TenantId = tenantId,
                OrderId = orderId,
                Amount = amount,
                Method = method,
                Provider = provider,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>Guarda a referência do provedor enquanto o pagamento ainda está pendente (ex.: aguardando webhook).</summary>
        public void SetProviderReference(string providerReference)
        {
            ProviderReference = providerReference;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool IsPending => Status == PaymentStatus.Pending;

        /// <summary>Pendente há mais tempo que o permitido (Pix não pago, 3D Secure abandonado).</summary>
        public bool IsExpired(TimeSpan timeout, DateTime utcNow) => IsPending && CreatedAt + timeout <= utcNow;

        public void Approve(string providerReference)
        {
            EnsurePending();
            Status = PaymentStatus.Approved;
            ProviderReference = providerReference;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reject(string reason, string? providerReference)
        {
            EnsurePending();
            Status = PaymentStatus.Rejected;
            FailureReason = reason;
            ProviderReference = providerReference;
            UpdatedAt = DateTime.UtcNow;
        }

        private void EnsurePending()
        {
            if (Status != PaymentStatus.Pending)
                throw new BusinessRuleException("payment_already_processed", "Pagamento já processado.");
        }
    }

    /// <summary>Chaves de idempotência de webhooks já processados (o provedor pode reenviar).</summary>
    public sealed class ProcessedWebhook
    {
        public string IdempotencyKey { get; set; } = null!;
        public Guid PaymentId { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}