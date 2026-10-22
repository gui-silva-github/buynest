namespace Buynest.SharedKernel.Tenancy {
    /// <summary>
    /// Quem é o tenant/usuário da requisição atual.
    /// Em HTTP vem das claims do JWT; em consumers de mensagens não existe (HasTenant = false).
    /// </summary>
    public interface ITenantContext {
        bool HasTenant { get; }
        Guid TenantId { get; }
        Guid? UserId { get; }
        string? UserName { get; }
    }
}