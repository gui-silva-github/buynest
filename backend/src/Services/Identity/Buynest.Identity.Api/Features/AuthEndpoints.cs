using Buynest.BuildingBlocks.Auth;
using Buynest.Identity.Api.Domain;
using Buynest.Identity.Api.Persistence;
using Buynest.Identity.Api.Services;
using Buynest.SharedKernel.Exceptions;
using Buynest.SharedKernel.Tenancy;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Buynest.Identity.Api.Features
{
    // DTOs como records (imutáveis): propriedades que o frontend envia
    public record LoginRequest(string Email, string Password);
    public record RegisterTenantRequest(string CompanyName, string Name, string Email, string Password);
    public record CreateUserRequest(string Name, string Email, string Password, string Role);
    public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserResponse User);
    public record UserResponse(Guid Id, string Name, string Email, string Role, Guid TenantId, string TenantName);

    public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public sealed class RegisterTenantRequestValidator : AbstractValidator<RegisterTenantRequest>
    {
        public RegisterTenantRequestValidator()
        {
            RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
                .Matches("[A-Z]").WithMessage("A senha precisa de uma letra maiúscula.")
                .Matches("[0-9]").WithMessage("A senha precisa de um número.");
        }
    }

    public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
    {
        public CreateUserRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
            RuleFor(x => x.Role).Must(r => r is Roles.Admin or Roles.Operator).WithMessage("Perfil inválido.");
        }
    }

    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var auth = app.MapGroup("/api/auth").WithTags("Auth");

            auth.MapPost("/login", async (
                LoginRequest request,
                IValidator<LoginRequest> validator,
                IdentityDbContext db,
                IPasswordHasher<User> hasher,
                ITokenService tokens,
                CancellationToken ct
            ) =>
            {
               await validator.ValidateAndThrowAsync(request, ct);

               var email = request.Email.Trim().ToLowerInvariant();
               var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);

               if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
                return Results.Unauthorized();

                var tenant = await db.Tenants.SingleAsync(t => t.Id == user.TenantId, ct);
                var token = tokens.CreateToken(user, tenant.Name);
                return Results.Ok(new AuthResponse(token.AccessToken, token.ExpiresAt, ToResponse(user, tenant.Name)));
            }).AllowAnonymous();

            auth.MapPost("/register", async (
                RegisterTenantRequest request,
                IValidator<RegisterTenantRequest> validator,
                IdentityDbContext db,
                IPasswordHasher<User> hasher,
                ITokenService tokens,
                CancellationToken ct
            ) =>
            {
                await validator.ValidateAndThrowAsync(request, ct);
                await EnsureEmailIsFreeAsync(db, request.Email, ct);

                var tenant = Tenant.Create(request.CompanyName);
                var admin = User.Create(tenant.Id, request.Name, request.Email, Roles.Admin);
                admin.SetPasswordHash(hasher.HashPassword(admin, request.Password));

                db.AddRange(tenant, admin);
                await db.SaveChangesAsync(ct);

                var token = tokens.CreateToken(admin, tenant.Name);
                return Results.Created($"/api/users/{admin.Id}",
                    new AuthResponse(token.AccessToken, token.ExpiresAt, ToResponse(admin, tenant.Name)));
            }).AllowAnonymous();

            auth.MapGet("/me", async (ITenantContext current, IdentityDbContext db, CancellationToken ct) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == current.UserId, ct)
                    ?? throw new NotFoundException("Usuário", current.UserId ?? Guid.Empty);
                var tenant = await db.Tenants.SingleAsync(t => t.Id == user.TenantId, ct);
                return Results.Ok(ToResponse(user, tenant.Name));
            }).RequireAuthorization();

            var users = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization(Policies.AdminOnly);

            users.MapGet("/", async (ITenantContext current, IdentityDbContext db, CancellationToken ct) =>
            {
                var tenantId = current.TenantId;
                var list = await db.Users
                    .Where(u => u.TenantId == tenantId)
                    .OrderBy(u => u.Name)
                    .Select(u => new { u.Id, u.Name, u.Email, u.Role, u.IsActive, u.CreatedAt })
                    .ToListAsync(ct);
                return Results.Ok(list);
            });

            users.MapPost("/", async (
                CreateUserRequest request,
                IValidator<CreateUserRequest> validator,
                ITenantContext current,
                IdentityDbContext db,
                IPasswordHasher<User> hasher,
                CancellationToken ct
            ) =>
            {
                await validator.ValidateAndThrowAsync(request, ct);
                await EnsureEmailIsFreeAsync(db, request.Email, ct);

                var user = User.Create(current.TenantId, request.Name, request.Email, request.Role);
                user.SetPasswordHash(hasher.HashPassword(user, request.Password));
                db.Users.Add(user);
                await db.SaveChangesAsync(ct);

                return Results.Created($"/api/users/{user.Id}", new { user.Id });
            });

            return app;
        }

        private static async Task EnsureEmailIsFreeAsync(IdentityDbContext db, string email, CancellationToken ct)
        {
            var normalized = email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == normalized, ct))
                throw new ConflictException("email_in_use", "Este e-mail já está em uso.");
        }

        private static UserResponse ToResponse(User user, string tenantName) =>
            new(user.Id, user.Name, user.Email, user.Role, user.TenantId, tenantName);
    }
}
