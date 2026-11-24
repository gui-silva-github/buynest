using Buynest.Catalog.Application.Abstractions;
using Buynest.Catalog.Domain.Products;
using Buynest.Contracts;
using Buynest.SharedKernel.Exceptions;
using Buynest.SharedKernel.Tenancy;
using MediatR;

namespace Buynest.Catalog.Application.Products.Commands
{
    public record DeleteProductCommand(Guid Id) : IRequest;

    public sealed class DeleteProductHandler(
        IProductRepository repository,
        ITenantContext tenant,
        IIntegrationEventPublisher publisher,
        IProductListCache cache
    ) : IRequestHandler<DeleteProductCommand>
    {
        public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
        {
            var product = await repository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException("Produto", request.Id);

            repository.Remove(product);
            await publisher.PublishAsync(new ProductDeleted(product.Id, product.TenantId), cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            await cache.InvalidateAsync(tenant.TenantId, cancellationToken);
        }
    }
}
