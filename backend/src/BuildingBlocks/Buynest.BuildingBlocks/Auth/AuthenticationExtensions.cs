using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Buynest.BuildingBlocks.Auth {
    public static class AuthenticationExtensions {
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration) {
            var section = configuration.GetSection(JwtOptions.Section);
            var options = section.Get<JwtOptions>() ?? new JwtOptions();

            if (options.Key.Length < 32)
                throw new InvalidOperationException("Configure 'Jwt:Key' com pelo menos 32 caracteres.");

            services.Configure<JwtOptions>(section);

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(jwt => 
                {
                    jwt.MapInboundClaims = false;
                    jwt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = options.Issuer,
                        ValidateAudience = true,
                        ValidAudience = options.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                        NameClaimType = BuynestClaims.Name,
                        RoleClaimType = BuynestClaims.Role
                    };

                    // WebSockets (SignalR) não enviam header Authorization: o token vem na query string
                    jwt.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context => 
                        {
                            var accessToken = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(access_token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                                context.Token = accessToken;
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorizationBuilder()
                .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(Roles.Admin));

            return services;
        }
    }
}