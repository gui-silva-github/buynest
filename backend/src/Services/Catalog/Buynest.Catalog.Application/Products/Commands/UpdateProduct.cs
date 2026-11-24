using Buynest.Catalog.Application.Abstractions;
using Buynest.Catalog.Domain.Products;
using Buynest.SharedKernel.Exceptions;
using Buynest.SharedKernel.Tenancy;
using FluentValidation;
using MediatR;

namespace Buynest.Catalog.Application.Products.Commands
{
    public record UpdateProductCommand(
        Guid Id, string Name, string? Description, decimal Price,
        ProductCategory Category, bool IsFeatured
    ) : IRequest;

    public sealed class UpdateProductValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.Category).IsInEnum();
        }
    }

    public sealed class UpdateProductHandler(
        IProductRepository repository,
        ITenantContext tenant,
        IProductListCache cache
    ) : IRequestHandler<UpdateProductCommand>
    {
        public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
        {
            var product = await repository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException("Produto", request.Id);

            product.Update(request.Name, request.Description, request.Price, request.Category, request.IsFeatured);

            await repository.SaveChangesAsync(cancellationToken);
            await cache.InvalidateAsync(tenant.TenantId, cancellationToken);
        }
    }
}
