using Buynest.BuildingBlocks;
using Buynest.BuildingBlocks.Messaging;
using Buynest.BuildingBlocks.Persistence;
using Buynest.Inventory.Api.Adapters.In.Http;
using Buynest.Inventory.Api.Adapters.In.Messaging;
using Buynest.Inventory.Api.Adapters.Out.Persistence;
using Buynest.Inventory.Api.Application.Ports.In;
using Buynest.Inventory.Api.Application.Ports.Out;
using Buynest.Inventory.Api.Application.UseCases;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("Inventory");

builder.Services.AddDbContext<InventoryDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// liga cada PORTA ao seu ADAPTER/implementação
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IInventoryQueries, InventoryQueries>();
builder.Services.AddScoped<IStockManagementUseCase, StockManagementService>();
builder.Services.AddScoped<IStockReservationUseCase, StockReservationService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddMessaging<InventoryDbContext>(builder.Configuration, "inventory", bus =>
{
   bus.AddConsumer<ProductCreatedConsumer>();
   bus.AddConsumer<ProductDeletedConsumer>();
   bus.AddConsumer<ReserveStockConsumer>();
   bus.AddConsumer<ReleaseStockConsumer>();
   bus.AddConsumer<ConfirmStockConsumer>(); 
});

var app = builder.Build();

app.UseServiceDefaults();
app.MapInventoryEndpoints();

await app.EnsureDatabaseAsync<InventoryDbContext>();

app.Run();