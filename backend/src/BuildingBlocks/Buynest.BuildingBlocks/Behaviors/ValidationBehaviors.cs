using FluentValidation;
using MediatR;

namespace Buynest.BuildingBlocks.Behaviors {
    /// <summary>
    /// Pipeline do MediatR (padrão Chain of Responsability): valida o request com FluentValidation
    /// ANTES de chegar no handler. Se falhar, o middleware devolve 400 com os erros por campo
    public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidation<TRequest>> validators) :
        IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) {
            if (!validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(validators.Select(v => v.ValidateAsnync(context, cancellationToken)));
            var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);

            return await next();
        }
    }
}