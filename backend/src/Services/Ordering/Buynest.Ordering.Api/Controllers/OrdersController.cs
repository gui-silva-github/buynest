using Buynest.Ordering.Api.Application;
using Buynest.Ordering.Api.Domain;
using Buynest.SharedKernel.Paging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buynest.Ordering.Api.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public sealed class OrdersController(IOrderService orders): ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null, [FromQuery] OrderStatus? status = null,
            CancellationToken ct = default
        ) =>
        Ok(await orders.ListAsync(new PagedRequest(pageIndex, pageSize, search), status, ct));

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct) => Ok(await orders.GetByIdAsync(id, ct));

        /// <summary>
        /// 202: Accepted: o pedido foi ACEITO, mas ainda será processado (estoque + pagamento).
        /// O status final chega depois via SignalR - consistência eventual.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Place(PlaceOrderRequest request, CancellationToken ct)
        {
            var id = await orders.PlaceOrderAsync(request, ct);
            return AcceptedAtAction(nameof(GetById), new { id }, new { id });
        }
    }
}