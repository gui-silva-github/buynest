using Buynest.BuildingBlocks.Messaging;
using Buynest.BuildingBlocks.Persistence;
using Buynest.Catalog.Domain.Products;
using Buynest.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Buynest.Catalog.Infrastructure.Persistence
{
    public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options, ITenantContext tenant) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductStatusHistory> ProductStatusHistory => Set<ProductStatusHistory>();

        // Avaliado a cada query: cada tenant só enxerga os próprios produtos
        private Guid? CurrentTenantId => tenant.HasTenant ? tenant.TenantId : null;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

            // Multi-tenant (banco compartilhado + filtro global por TenantId)
            modelBuilder.Entity<Product>().HasQueryFilter(p => CurrentTenantId == null || p.TenantId == CurrentTenantId);

            modelBuilder.AddMessagingTables();
            modelBuilder.UseClientGeneratedIds();
        }    
    }
}