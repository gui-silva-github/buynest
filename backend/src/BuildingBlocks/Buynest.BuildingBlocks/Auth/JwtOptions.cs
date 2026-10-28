namespace Buynest.BuildingBlocks.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; init; } = "buynest-identity";
    public string Audience { get; init; } = "buynest";
    /// <summary>Chave simétrica (mín. 32 caracteres). Em produção: Azure Key Vault / variável de ambiente.</summary>
    public string Key { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 120;
}

public static class BuynestClaims
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Email = "email";
    public const string Role = "role";
    public const string TenantId = "tenant_id";
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";
}

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
}
