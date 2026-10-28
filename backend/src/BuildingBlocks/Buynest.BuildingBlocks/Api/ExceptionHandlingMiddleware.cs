using Buynest.SharedKernel.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Buynest.BuildingBlocks.Api {
    /// <summary>
    /// Middleware global: transforma exceções em respostas HTTP padronizadas (ApiErrorResponse).
    /// Evita try/catch espalhado nos controllers.
    /// <summary>
    public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger) {
        public async Task InvokeAsync(HttpContext context) {
            try {
                await next(context);
            }
            catch (Exception ex) when (!context.Response.HasStarted) {
                await HandleAsync(context, ex);
            }
        }

        private async Task HandleAsync(HttpContext context, Exception exception) {
            (int Status, ApiErrorResponse Body) result = exception switch
            {
                ValidationException v => (StatusCodes.Status400BadRequest, new ApiErrorResponse(
                    "validation_error",
                    "Um ou mais campos são inválidos.",
                    v.Errors
                        .GroupBy(e => ToCamelCase(e.PropertyName))
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))),
                NotFoundException nf => (StatusCodes.Status404NotFound, new ApiErrorResponse("not_found", nf.Message)),
                BusinessRuleException br => (StatusCodes.Status422UnprocessableEntity, new ApiErrorResponse(br.Code, br.Message)),
                ConflictException cf => (StatusCodes.Status409Conflict, new ApiErrorResponse(cf.Code, cf.Message)),
                UnauthorizedAccessException ua => (StatusCodes.Status401Unauthorized, new ApiErrorResponse("unauthorized", ua.Message)),
                DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, new ApiErrorResponse(
                    "concurrency_conflict",
                    "O registro foi alterado por outra pessoa. Recarregue e tente novamente.")),
                _ => (StatusCodes.Status500InternalServerError, new ApiErrorResponse("internal_error", "Ocorreu um erro inesperado."))    
            };

            if (result.Status >= 500)
                logger.LogError(exception, "Erro não tratado em {Path}", context.Request.Path);
            else 
                logger.LogWarning("Requisição {Path} falhou com {Status}: {Message}", context.Request.Path, result.Status, exception.Message);

            context.Response.StatusCode = result.Status;
            await context.Response.WriteAsJsonAsync(result.Body with { TraceId = context.TraceIdentifier });
        }

        private static string ToCamelCase(string value) =>
            string.IsNullOrEmpty(value) ? value : char.ToLowerInvariant(value[0]) + value[1..];
    }
}