using Buynest.BuildingBlocks;
using Buynest.BuildingBlocks.Persistence;
using Buynest.Identity.Api.Domain;
using Buynest.Identity.Api.Features;
using Buynest.Identity.Api.Persistence;
using Buynest.Identity.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("Identity");

builder.Services.AddDbContext<IdentityDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

app.UseServiceDefaults();
app.MapAuthEndpoints();

await app.EnsureDatabaseAsync<IdentityDbContext>();
await DevelopmentSeeder.SeedAsync(app);

app.Run();
