using FlashSales.Application.Ports;
using FlashSales.Domain.Catalog;
using FlashSales.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;

namespace FlashSales.Infrastructure.Persistence;

/// <summary>
/// Write-model repository + stock authority. Overselling is prevented here:
/// the decrement is a single atomic conditional UPDATE — under any number of
/// concurrent checkouts the database serializes row access, and the WHERE
/// clause refuses the decrement once stock runs out. No read-modify-write
/// window exists.
/// </summary>
public sealed class OfferRepository(FlashSalesDbContext db) : IOfferRepository, IStockAuthority, IStockReader
{
    public async Task<Offer?> GetByIdAsync(Guid offerId, CancellationToken ct)
    {
        var entity = await db.Offers.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == offerId, ct);
        return entity?.ToDomain();
    }

    public async Task<int?> TryDecrementStockAsync(Guid offerId, int quantity, CancellationToken ct)
    {
        // Data-modifying CTE so the statement stays composable for EF's SqlQuery.
        var remaining = await db.Database.SqlQuery<int>($@"
            WITH updated AS (
                UPDATE inventory.offers
                SET stock = stock - {quantity}
                WHERE id = {offerId} AND stock >= {quantity}
                RETURNING stock
            )
            SELECT stock AS ""Value"" FROM updated")
            .ToListAsync(ct);

        return remaining.Count == 0 ? null : remaining[0];
    }

    public async Task<int> RestoreStockAsync(Guid offerId, int quantity, CancellationToken ct)
    {
        var restored = await db.Database.SqlQuery<int>($@"
            WITH updated AS (
                UPDATE inventory.offers
                SET stock = stock + {quantity}
                WHERE id = {offerId}
                RETURNING stock
            )
            SELECT stock AS ""Value"" FROM updated")
            .ToListAsync(ct);

        return restored.Count == 0
            ? throw new InvalidOperationException($"Cannot restore stock of unknown offer {offerId}")
            : restored[0];
    }

    public async Task<int?> ReadStockAsync(Guid offerId, CancellationToken ct)
    {
        var stock = await db.Offers.AsNoTracking()
            .Where(o => o.Id == offerId)
            .Select(o => (int?)o.Stock)
            .FirstOrDefaultAsync(ct);
        return stock;
    }
}
