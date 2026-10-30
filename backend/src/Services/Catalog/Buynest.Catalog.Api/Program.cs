using Buynest.BuildingBlocks;
using Buynest.BuildingBlocks.Persistence;
using Buynest.Catalog.Application;
using Buynest.Catalog.Infrastructure;
using Buynest.Catalog.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("Catalog");

// Cada camada registra as próprias dependências (DependencyInjection.cs)
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();

var app = builder.Build();

app.UseServiceDefaults();
app.MapControllers();

await app.EnsureDatabaseAsync<CatalogDbContext>();

app.Run();