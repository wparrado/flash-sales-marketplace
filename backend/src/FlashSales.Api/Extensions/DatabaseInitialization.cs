using FlashSales.SharedKernel;
using FlashSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlashSales.Api.Extensions;

public static class DatabaseInitialization
{
    /// <summary>
    /// Creates the schema and seeds demo flash sales. EnsureCreated keeps the
    /// technical test self-contained; a production deployment switches to
    /// versioned EF migrations executed by the delivery pipeline.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlashSalesDbContext>();

        await db.Database.EnsureCreatedAsync();

        if (app.Configuration.GetValue("Database:SeedDemoData", true) && !await db.Offers.AnyAsync())
            await SeedDemoOffersAsync(db);
    }

    private static async Task SeedDemoOffersAsync(FlashSalesDbContext db)
    {
        var now = DateTimeOffset.UtcNow;
        var sellers = (Standard: Guid.NewGuid(), Premium: Guid.NewGuid());

        (string Name, string Description, decimal Price, int Stock, Guid Seller, SellerTier Tier)[] catalog =
        [
            ("Nintendo Switch 2", "Next-gen hybrid console, launch edition", 449.99m, 25, sellers.Standard, SellerTier.Standard),
            ("PlayStation 5 Pro", "4K 120fps gaming powerhouse", 699.99m, 15, sellers.Premium, SellerTier.Premium),
            ("Mechanical Keyboard TKL", "Hot-swappable switches, RGB", 89.99m, 120, sellers.Standard, SellerTier.Standard),
            ("Wireless Gaming Mouse", "8K polling, 59g ultralight", 129.99m, 80, sellers.Standard, SellerTier.Standard),
            ("4K OLED Monitor 27\"", "240Hz, 0.03ms response time", 899.99m, 10, sellers.Premium, SellerTier.Premium),
            ("Noise Cancelling Headphones", "Studio-grade ANC over-ear", 279.99m, 45, sellers.Premium, SellerTier.Premium),
            ("Cámara Réflex Canon", "24MP DSLR with 18-55mm kit lens", 549.99m, 8, sellers.Standard, SellerTier.Standard),
            ("Drone DJI Mini", "Sub-249g 4K drone, 30min flight", 459.99m, 12, sellers.Premium, SellerTier.Premium)
        ];

        db.Offers.AddRange(catalog.Select(item => new OfferEntity
        {
            Id = Guid.NewGuid(),
            SellerId = item.Seller,
            Name = item.Name,
            Description = item.Description,
            PriceAmount = item.Price,
            Currency = "USD",
            Stock = item.Stock,
            StartsAt = now.AddHours(-1),
            EndsAt = now.AddHours(24),
            SellerTier = item.Tier
        }));

        await db.SaveChangesAsync();
    }
}
