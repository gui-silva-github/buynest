using Buynest.Catalog.Application.Abstractions;
using Buynest.Catalog.Domain.Products;
using Buynest.SharedKernel.Exceptions;
using Buynest.SharedKernel.Tenancy;
using FluentValidation;
using MediatR;

namespace Buynest.Catalog.Application.Products.Commands
{
    public record ChangeProductStatusCommand(Guid Id, ProductStatus Status, string? Reason) : IRequest;

    public sealed class ChangeProductStatusValidator : AbstractValidator<ChangeProductStatusCommand>
    {
        public ChangeProductStatusValidator()
        {
            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.Reason).MaximumLength(500);
            RuleFor(x => x.Reason).NotEmpty()
                .When(x => x.Status == ProductStatus.Blocked)
                .WithMessage("Informe o motivo do bloqueio.");
        }
    }

    public sealed class ChangeProductStatusHandler(
        IProductRepository repository,
        ITenantContext tenant,
        IProductListCache cache
    ) : IRequestHandler<ChangeProductStatusCommand>
    {
        public async Task Handle(ChangeProductStatusCommand request, CancellationToken cancellationToken)
        {
            var product = await repository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException("Produto", request.Id);
            
            product.ChangeStatus(request.Status, request.Reason, tenant.UserName);

            await repository.SaveChangesAsync(cancellationToken);
            await cache.InvalidateAsync(tenant.TenantId, cancellationToken);
        }
    }
}
