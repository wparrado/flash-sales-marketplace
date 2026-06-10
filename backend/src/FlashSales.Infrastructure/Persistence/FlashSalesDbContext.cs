using Microsoft.EntityFrameworkCore;

namespace FlashSales.Infrastructure.Persistence;

public class FlashSalesDbContext(DbContextOptions<FlashSalesDbContext> options) : DbContext(options)
{
    public DbSet<OfferEntity> Offers => Set<OfferEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explicit snake_case names: the atomic stock decrement uses raw SQL,
        // so table/column names are part of the repository contract.
        modelBuilder.Entity<OfferEntity>(offer =>
        {
            offer.ToTable("offers");
            offer.HasKey(o => o.Id);
            offer.Property(o => o.Id).HasColumnName("id");
            offer.Property(o => o.SellerId).HasColumnName("seller_id");
            offer.Property(o => o.Name).HasColumnName("name").HasMaxLength(200);
            offer.Property(o => o.Description).HasColumnName("description").HasMaxLength(2000);
            offer.Property(o => o.PriceAmount).HasColumnName("price_amount").HasPrecision(18, 2);
            offer.Property(o => o.Currency).HasColumnName("currency").HasMaxLength(3);
            offer.Property(o => o.Stock).HasColumnName("stock");
            offer.Property(o => o.StartsAt).HasColumnName("starts_at");
            offer.Property(o => o.EndsAt).HasColumnName("ends_at");
            offer.Property(o => o.SellerTier).HasColumnName("seller_tier").HasConversion<string>().HasMaxLength(20);
            offer.HasIndex(o => o.EndsAt);
        });

        modelBuilder.Entity<OrderEntity>(order =>
        {
            order.ToTable("orders");
            order.HasKey(o => o.Id);
            order.Property(o => o.Id).HasColumnName("id");
            order.Property(o => o.BuyerId).HasColumnName("buyer_id");
            order.Property(o => o.SellerId).HasColumnName("seller_id");
            order.Property(o => o.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            order.Property(o => o.PaymentReference).HasColumnName("payment_reference").HasMaxLength(100);
            order.Property(o => o.FailureReason).HasColumnName("failure_reason").HasMaxLength(500);
            order.Property(o => o.PlacedAt).HasColumnName("placed_at");
            order.Property(o => o.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2);
            order.Property(o => o.Discount).HasColumnName("discount").HasPrecision(18, 2);
            order.Property(o => o.Tax).HasColumnName("tax").HasPrecision(18, 2);
            order.Property(o => o.Total).HasColumnName("total").HasPrecision(18, 2);
            order.Property(o => o.Commission).HasColumnName("commission").HasPrecision(18, 2);
            order.Property(o => o.SellerPayout).HasColumnName("seller_payout").HasPrecision(18, 2);
            order.Property(o => o.Currency).HasColumnName("currency").HasMaxLength(3);
            order.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId);
        });

        modelBuilder.Entity<OrderLineEntity>(line =>
        {
            line.ToTable("order_lines");
            line.HasKey(l => l.Id);
            line.Property(l => l.Id).HasColumnName("id");
            line.Property(l => l.OrderId).HasColumnName("order_id");
            line.Property(l => l.OfferId).HasColumnName("offer_id");
            line.Property(l => l.OfferName).HasColumnName("offer_name").HasMaxLength(200);
            line.Property(l => l.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
            line.Property(l => l.Quantity).HasColumnName("quantity");
        });
    }
}
