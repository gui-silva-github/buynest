using System.Text.Json.Serialization;
using Buynest.BuildingBlocks.Api;
using Buynest.BuildingBlocks.Auth;
using Buynest.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Buynest.BuildingBlocks {
    /// <summary>
    /// Extension methods de DI ("DependencyInjection.cs") compartilhados por todos os serviços.
    /// Cada Program.cs fica curto: builder.AddServiceDefaults("Catalog") + app.UseServiceDefaults().
    public static class ServiceDefaults {
        public static WebApplicationBuilder AddServiceDefaults(this WebApplicationBuilder builder, string serviceName) {
            var services = builder.Services;

            services.AddHttpContextAccessor();
            services.AddScoped<ITenantContext, HttpTenantContext>();
            services.AddJwtAuthentication(builder.Configuration);
            services.AddHealthChecks();

            // enums como texto ("Active") em vez de número (1) - controllers e minimal APIs
            services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o =>
                o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
            );

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(swagger =>
            {
                swagger.SwaggerDoc("v1", new OpenApiInfo { Title = $"Buynest {serviceName} API", Version = "v1" });
                swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Description = "Cole apenas o token (sem 'Bearer ')."
                });
                swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            }
            );

            return builder;
        }

        public static WebApplication UseServiceDefaults(this WebAppication app) {
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            if (!app.Environment.IsProduction()) {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthentication();
            app.UseAuthorization();
            app.MapHealthChecks("/health");

            return app;
        }
    }
}
