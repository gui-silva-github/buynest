namespace Buynest.SharedKernel.Exceptions {
    /// <summary>Violação de regra de negócio -> HTTP 422.</summary>
    public class BusinessRuleException(string code, string message): Exception(message) {
        public string Code { get; } = code;
    }

    /// <summary>Recurso inexistente -> HTTP 404</summary>
    public class NotFoundException(string resource, object key) : Exception($"{resource} '{key}' não encontrado.") {
        public string Resource { get; } = resource;
    }

    /// <summary>Conflito (ex.: SKU duplicado, concorrência) -> HTTP 409.</summary>
    public class ConflictException(string code, string message): Exception(message) {
        public string Code { get; } = code;
    }
}