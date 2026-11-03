using Buynest.BuildingBlocks;
using Buynest.BuildingBlocks.Messaging;
using Buynest.BuildingBlocks.Persistence;
using Buynest.Ordering.Api.Application;
using Buynest.Ordering.Api.Infrastructure;
using Buynest.Ordering.Api.Realtime;
using Buynest.Ordering.Api.Saga;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("Ordering");

builder.Services.AddDbContext<OrderingDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IOrderNotifier, SignalROrderNotifier>();
builder.Services.AddScoped<OrderTimelineRecorder>();

// HttpClient tipado + Polly (retry exponencial, circuit breaker, timeout) via Microsoft.Extensions.Http.Resilience
builder.Services.AddTransient<ForwardAuthorizationHandler>();
builder.Services
    .AddHttpClient<ICatalogClient, CatalogClient>(c =>
        c.BaseAddress = new Uri(builder.Configuration["Services:Catalog"] ?? "http://localhost:5002/"))
    .AddHttpMessageHandler<ForwardAuthorizationHandler>()
    .AddStandardResilienceHandler();

builder.Services.AddMessaging<OrderingDbContext>(builder.Configuration, "ordering", bus =>
{   
    bus.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<OrderingDbContext>();
            r.UsePostgres();
        });

    bus.AddConsumer<OrderConfirmedConsumer>();
    bus.AddConsumer<OrderCancelledConsumer>();

    // linha do tempo: observam as mensagens da saga (cada um na sua fila)
    bus.AddConsumer<StockReservedTimelineConsumer>();
    bus.AddConsumer<StockReservationFailedTimelineConsumer>();
    bus.AddConsumer<PaymentRequestedTimelineConsumer>();
    bus.AddConsumer<PaymentApprovedTimelineConsumer>();
    bus.AddConsumer<PaymentRejectedTimelineConsumer>();
    bus.AddConsumer<StockConfirmedTimelineConsumer>();
    bus.AddConsumer<StockReleasedTimelineConsumer>();
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseServiceDefaults();
app.MapControllers();
app.MapHub<OrdersHub>("/hubs/orders");

await app.EnsureDatabaseAsync<OrderingDbContext>();
await app.EnsureOrderTimelineTableAsync();

app.Run();