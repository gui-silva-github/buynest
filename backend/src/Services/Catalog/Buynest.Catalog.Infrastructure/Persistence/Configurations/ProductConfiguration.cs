using Buynest.Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Buynest.Catalog.Infrastructure.Persistence.Configurations
{
    public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("products");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Sku).HasMaxLength(30).IsRequired();
            builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
            builder.Property(p => p.Description).HasMaxLength(1000);
            builder.Property(p => p.Price).HasPrecision(18, 2);
            builder.Property(p => p.Category).HasConversion<string>().HasMaxLength(30);
            builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            builder.Property(p => p.StatusReason).HasMaxLength(500);

            builder.HasIndex(p => new { p.TenantId, p.Sku }).IsUnique();
            builder.HasIndex(p => new { p.TenantId, p.Name });

            builder.HasMany(p => p.StatusHistory)
                .WithOne()
                .HasForeignKey(h => h.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(p => p.StatusHistory).UsePropertyAccessMode(PropertyAccessMode.Field);

            builder.Ignore(p => p.IsAvailableForSale);
        }
    }

    public sealed class ProductStatusHistoryConfiguration : IEntityTypeConfiguration<ProductStatusHistory>
    {
        public void Configure(EntityTypeBuilder<ProductStatusHistory> builder)
        {
            builder.ToTable("product_status_history");
            builder.HasKey(h => h.Id);
            builder.Property(h => h.FromStatus).HasConversion<string>().HasMaxLength(20);
            builder.Property(h => h.ToStatus).HasConversion<string>().HasMaxLength(20);
            builder.Property(h => h.Reason).HasMaxLength(500);
            builder.Property(h => h.ChangedBy).HasMaxLength(150);
        }
    }
}