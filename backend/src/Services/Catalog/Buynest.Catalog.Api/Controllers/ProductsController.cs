using Buynest.BuildingBlocks.Auth;
using Buynest.Catalog.Application.Abstractions;
using Buynest.Catalog.Application.Products;
using Buynest.Catalog.Application.Products.Commands;
using Buynest.Catalog.Application.Products.Queries;
using Buynest.Catalog.Domain.Products;
using Buynest.SharedKernel.Paging;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Buynest.Catalog.Api.Controllers
{
    // Body dos endpoints de escrita (sem o Id, que vem da rota)
    public record UpdateProductBody(string Name, string? Description, decimal Price, ProductCategory Category, bool IsFeatured);
    public record ChangeStatusBody(ProductStatus Status, string? Reason);

    /// <summary>
    /// CRUD de produtos
    /// List -> GET /api/products?pageIndex=1&amp;pageSize=10&amp;search=... (GetAll / GetSearch)
    /// Details -> GET /api/products/{id} (GetBy)
    /// Create -> POST /api/products (Post)
    /// Edit -> PUT /api/products/{id} (Put)
    /// Status -> PATCH /api/products/{id}/status (Modal de Bloqueio)
    /// History -> GET /api/products/{id}/history (Modal de Histórico)
    /// Delete -> DELETE /api/products/{id}
    /// </summary>
    
    [ApiController]
    [Route("api/products")]
    [Authorize]
    public sealed class ProductsController(ISender sender) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<PagedResult<ProductListItem>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> List(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] ProductStatus? status = null,
            [FromQuery] ProductCategory? category = null,
            CancellationToken cancellationToken = default
        )
        {
            var filter = new ProductFilter(new PagedRequest(pageIndex, pageSize, search), status, category);
            return Ok(await sender.Send(new ListProductsQuery(filter), cancellationToken));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
            Ok(await sender.Send(new GetProductByIdQuery(id), cancellationToken));

        [HttpGet("{id:guid}/history")]
        public async Task<IActionResult> GetHistory(Guid id, CancellationToken cancellationToken) =>
            Ok(await sender.Send(new GetProductStatusHistoryQuery(id), cancellationToken));

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductCommand command, CancellationToken cancellationToken)
        {
            var id = await sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, UpdateProductBody body, CancellationToken cancellationToken)
        {
            await sender.Send(new UpdateProductCommand(id, body.Name, body.Description, body.Price, body.Category, body.IsFeatured), cancellationToken);
            return NoContent();
        }

        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, ChangeStatusBody body, CancellationToken cancellationToken)
        {
            await sender.Send(new ChangeProductStatusCommand(id, body.Status, body.Reason), cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Policies.AdminOnly)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            await sender.Send(new DeleteProductCommand(id), cancellationToken);
            return NoContent();
        }
    }
}