using FlashSales.Application.Contracts;
using FlashSales.Application.Ports;
using Microsoft.EntityFrameworkCore;

namespace FlashSales.Infrastructure.Persistence;

/// <summary>CQRS read side: no-tracking projections straight into read DTOs.</summary>
public sealed class OfferReadRepository(FlashSalesDbContext db, TimeProvider clock) : IOfferReadRepository
{
    public async Task<IReadOnlyList<OfferSummary>> GetActiveAsync(int limit, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        return await db.Offers.AsNoTracking()
            .Where(o => o.StartsAt <= now && o.EndsAt > now)
            .OrderBy(o => o.EndsAt)
            .Take(limit)
            .Select(o => new OfferSummary(
                o.Id, o.Name, o.Description, o.PriceAmount, o.Currency, o.Stock, o.EndsAt))
            .ToListAsync(ct);
    }

    public async Task<OfferSummary?> GetByIdAsync(Guid offerId, CancellationToken ct) =>
        await db.Offers.AsNoTracking()
            .Where(o => o.Id == offerId)
            .Select(o => new OfferSummary(
                o.Id, o.Name, o.Description, o.PriceAmount, o.Currency, o.Stock, o.EndsAt))
            .FirstOrDefaultAsync(ct);
}
