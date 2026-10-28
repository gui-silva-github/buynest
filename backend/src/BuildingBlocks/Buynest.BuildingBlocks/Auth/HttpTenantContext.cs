using Buynest.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Buynest.BuildingBlocks.Auth;

/// <summary>Lê tenant e usuário das claims do JWT da requisição atual.</summary>
public sealed class HttpTenantContext(IHttpContextAccessor accessor) : ITenantContext
{
    private Guid? ParseClaim(string type) =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirst(type)?.Value, out var id) ? id : null;

    public bool HasTenant => ParseClaim(BuynestClaims.TenantId).HasValue;

    public Guid TenantId => ParseClaim(BuynestClaims.TenantId)
        ?? throw new UnauthorizedAccessException("Tenant não identificado no token.");

    public Guid? UserId => ParseClaim(BuynestClaims.Subject);

    public string? UserName => accessor.HttpContext?.User.FindFirst(BuynestClaims.Name)?.Value;
}
