using Buynest.Catalog.Application.Abstractions;
using Buynest.Catalog.Domain.Products;
using Buynest.Contracts;
using Buynest.SharedKernel.Exceptions;
using Buynest.SharedKernel.Tenancy;
using FluentValidation;
using MediatR;

namespace Buynest.Catalog.Application.Products.Commands
{
    // COMMAND: muda estado. Request + Validator + Handler no mesmo arquivo = fácil de encontrar
    public record CreateProductCommand(
        string Sku, string Name, string? Description, decimal Price,
        ProductCategory Category, bool IsFeatured
    ) : IRequest<Guid>;

    public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
    {
        // Expressão regular: SKU com letras, número e hífen (ex.: NB-ASUS-001)
        public CreateProductValidator()
        {
            RuleFor(x => x.Sku).NotEmpty().MaximumLength(30)
                .Matches("^[A-Za-z0-9-]+$").WithMessage("SKU aceita apenas letras, números e hífen.");
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.Category).IsInEnum();
        }
    }

    public sealed class CreateProductHandler(
        IProductRepository repository,
        ITenantContext tenant,
        IIntegrationEventPublisher publisher,
        IProductListCache cache
    ) : IRequestHandler<CreateProductCommand, Guid>
    {
        public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            if (await repository.SkuExistsAsync(request.Sku, null, cancellationToken))
                throw new ConflictException("sku_in_use", $"Já existe um produto com o SKU '{request.Sku.ToUpperInvariant()}'.");

            var product = Product.Create(
                tenant.TenantId, request.Sku, request.Name, request.Description,
                request.Price, request.Category, request.IsFeatured, tenant.UserName
            );

            repository.Add(product);

            // Outbox: o evento é gravado na MESMA transação do produto e enviado ao RabbitMQ depois
            await publisher.PublishAsync(
                new ProductCreated(product.Id, product.TenantId, product.Sku, product.Name, product.Price),
                cancellationToken);

            await repository.SaveChangesAsync(cancellationToken);
            await cache.InvalidateAsync(tenant.TenantId, cancellationToken);

            return product.Id;
        }
    }
}
