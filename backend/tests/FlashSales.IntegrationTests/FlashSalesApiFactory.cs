using FlashSales.Domain.Catalog;
using FlashSales.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace FlashSales.IntegrationTests;

/// <summary>
/// Boots the real composition root (Program.cs) against the Testcontainers
/// database: HTTP adapter → use cases → EF Core → PostgreSQL, nothing faked.
/// </summary>
public sealed class FlashSalesApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", connectionString);
        builder.UseSetting("Database:SeedDemoData", "false");
        // Generous bulkhead so parallel race tests measure stock atomicity,
        // not queue rejections. The 503 path gets its own dedicated factory.
        builder.UseSetting("Checkout:PermitLimit", "50");
        builder.UseSetting("Checkout:QueueLimit", "100");
    }

    public async Task<Offer> SeedOfferAsync(Offer offer)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlashSalesDbContext>();
        db.Offers.Add(OfferEntity.FromDomain(offer));
        await db.SaveChangesAsync();
        return offer;
    }

    public async Task<int> ReadStockAsync(Guid offerId)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlashSalesDbContext>();
        return (await db.Offers.FindAsync(offerId))!.Stock;
    }
}
