namespace Buynest.BuildingBlocks.Api {
    /// <summary>
    /// Schema ÚNICO de erro de todas as APIs. O front usa "errors" para marcar campos
    /// do formulário e "message" para exibir no modal de erro.
    /// </summary>
    public record ApiErrorResponse(string Code, string Message, IDictionary<string, string[]>? Errors = null) {
        public string? TraceId { get; init; }
    }
}