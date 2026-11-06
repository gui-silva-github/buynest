using Buynest.BuildingBlocks;
using Buynest.BuildingBlocks.Messaging;
using Buynest.BuildingBlocks.Persistence;
using Buynest.Payment.Api.Features.Checkout;
using Buynest.Payment.Api.Features.ListPayments;
using Buynest.Payment.Api.Features.ProcessPayment;
using Buynest.Payment.Api.Features.ReconcilePayments;
using Buynest.Payment.Api.Features.StripeWebhook;
using Buynest.Payment.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("Payment");

builder.Services.AddDbContext<PaymentDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

var stripeSection = builder.Configuration.GetSection(StripeSettings.Section);
var stripe = stripeSection.Get<StripeSettings>() ?? new StripeSettings();

var stripeErrors = stripe.Validate().ToList();
if (stripeErrors.Count > 0) 
    throw new InvalidOperationException(
        "Configuração do Stripe inválida:\n - " + string.Join("\n - ", stripeErrors) +
        "\nDocker: preencha STRIPE_SECRET_KEY e STRIPE_PUBLISHABLE_KEY no .env." +
        "\nLocal: dotnet user-secrets set \"Stripe:SecretKey\" \"sk_test_...\" (e \"Stripe:PublishableKey\")."
    );

builder.Services.Configure<StripeSettings>(stripeSection);

// o SDK do Stripe já faz retry de falhas de rede reaproveitando a IdempotencyKey (seguro para POST)
StripeConfiguration.MaxNetworkRetries = 2;
builder.Services.AddSingleton(new StripeClient(stripe.SecretKey)); // thread-safe: 1 instância para a app
builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>();
builder.Services.AddHostedService<PendingPaymentsReconciler>();

builder.Services.AddMessaging<PaymentDbContext>(builder.Configuration, "payment", bus =>
{
    bus.AddConsumer<RequestPaymentConsumer>();
});

var app = builder.Build();

app.Logger.LogInformation("Stripe em modo {Mode}; webhook {Webhook}; pendentes expiram em {Timeout} min.",
    stripe.IsLiveMode ? "LIVE (cobranças reais)" : "TESTE",
    string.IsNullOrEmpty(stripe.WebhookSecret) ? "DESLIGADO (só reconciliação)" : "ligado",
    stripe.PendingTimeoutMinutes
);

app.UseServiceDefaults();
app.MapStripeWebhook();
app.MapCheckout();
app.MapListPayments();

await app.EnsureDatabaseAsync<PaymentDbContext>();

app.Run();
