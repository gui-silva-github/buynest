using Buynest.BuildingBlocks.Messaging;
using Buynest.BuildingBlocks.Persistence;
using Buynest.Payment.Api.Domain;
using Buynest.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;
using PaymentEntity = Buynest.Payment.Api.Domain.Payment;

namespace Buynest.Payment.Api.Persistence
{
    public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options, ITenantContext tenant): DbContext(options)
    {
        public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();
        public DbSet<ProcessedWebhook> ProcessedWebhooks => Set<ProcessedWebhook>();

        private Guid? CurrentTenantId => tenant.HasTenant ? tenant.TenantId : null;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentEntity>(e =>
            {
               e.ToTable("payments");
               e.Property(x => x.Amount).HasPrecision(18, 2);
               e.Property(x => x.Method).HasMaxLength(30);
               e.Property(x => x.Provider).HasMaxLength(20);
               e.Ignore(x => x.IsPending);
               e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
               e.Property(x => x.ProviderReference).HasMaxLength(100);
               // concorrência otimista com a coluna de sistema xmin do Postgres (não cria coluna):
               // webhook e reconciliação nunca sobrescrevem a decisão um do outro
               e.Property<uint>("Version").IsRowVersion();
               e.Property(x => x.FailureReason).HasMaxLength(500);
               e.HasIndex(x => x.OrderId).IsUnique(); // 1 pagamento por pedido (idempotência no banco)
               e.HasQueryFilter(x => CurrentTenantId == null || x.TenantId == CurrentTenantId); 
            });

            modelBuilder.Entity<ProcessedWebhook>(e =>
            {
                e.ToTable("processed_webhooks");
                e.HasKey(x => x.IdempotencyKey);
                e.Property(x => x.IdempotencyKey).HasMaxLength(100);
            });

            modelBuilder.AddMessagingTables();
            modelBuilder.UseClientGeneratedIds();
        }
    }
}