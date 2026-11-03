using Buynest.SharedKernel.Domain;
using Buynest.SharedKernel.Exceptions;

namespace Buynest.Ordering.Api.Domain
{
    public enum OrderStatus
    {
        Pending = 1, // aguardando estoque + pagamento (saga em andamento)
        Confirmed = 2,
        Cancelled = 3
    }

    public sealed class Order : Entity, IAggregateRoot, ITenantEntity
    {
        private readonly List<OrderItem> _items = [];

        private Order() {}

        public Guid TenantId { get; private set; }
        public string Number { get; private set; } = null!;
        public Guid CustomerId { get; private set; }
        public string CustomerName { get; private set; } = null!;
        public string PaymentMethod { get; private set; } = null!;
        public OrderStatus Status { get; private set; }
        public decimal Total { get; private set; }
        public string? CancelReason { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; } 

        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        public static Order Create(Guid tenantId, Guid customerId, string customerName, string paymentMethod,
            IEnumerable<(Guid ProductId, string Sku, string Name, decimal UnitPrice, int Quantity)> items)
        {
            var order = new Order
            {
                TenantId = tenantId,
                CustomerId = customerId,
                CustomerName = customerName,
                PaymentMethod = paymentMethod,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            order.Number = $"PED-{order.CreatedAt:yyMMdd}-{order.Id.ToString("N")[^6..].ToUpperInvariant()}";

            foreach (var (productId, sku, name, unitPrice, quantity) in items)
                order._items.Add(OrderItem.Create(order.Id, productId, sku, name, unitPrice, quantity));
        
            if (order._items.Count == 0)
                throw new BusinessRuleException("empty_order", "O pedido precisa de pelo menos um item.");

            order.Total = order._items.Sum(i => i.Subtotal);
            return order;
        }

        /// <summary>Idempotente: a mesma mensagem pode chegar mais de uma vez.</summary>
        public void Confirm()
        {
            if (Status == OrderStatus.Confirmed) return;
            if (Status != OrderStatus.Pending)
                throw new BusinessRuleException("invalid_transition", "Somente pedidos pendentes podem ser confirmados.");
            Status = OrderStatus.Confirmed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel(string reason)
        {   
            if (Status == OrderStatus.Cancelled) return;
            if (Status != OrderStatus.Pending)
                throw new BusinessRuleException("invalid_transition", "Somente pedidos pendentes podem ser cancelados.");
            Status = OrderStatus.Cancelled;
            CancelReason = reason;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public sealed class OrderItem : Entity
    {
        private OrderItem() {}

        public Guid OrderId { get; private set; }
        public Guid ProductId { get; private set; }
        public string Sku { get; private set; } = null!;
        public string ProductName { get; private set; } = null!;
        public decimal UnitPrice { get; private set; }
        public int Quantity { get; private set; }
        public decimal Subtotal => UnitPrice * Quantity;

        internal static OrderItem Create(Guid orderId, Guid productId, string sku, string name, decimal unitPrice, int quantity)
        {
            if (quantity <= 0)
                throw new BusinessRuleException("invalid_quantity", "Quantidade deve ser maior que zero.");

            return new OrderItem
            {
                OrderId = orderId,
                ProductId = productId,
                Sku = sku,
                ProductName = name,
                UnitPrice = unitPrice,
                Quantity = quantity
            };
        }
    }
}