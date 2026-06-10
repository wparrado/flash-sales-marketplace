using FlashSales.Application.Ports;

namespace FlashSales.Infrastructure.Search;

/// <summary>
/// Synchronous in-process propagation of committed stock changes to the search
/// read model. In a distributed deployment this adapter is replaced by an
/// outbox + message bus; the IOfferIndexUpdater port does not change.
/// </summary>
public sealed class InProcessOfferIndexUpdater(
    ISearchEngine searchEngine,
    IOfferReadRepository reads) : IOfferIndexUpdater
{
    public async Task OfferStockChangedAsync(Guid offerId, int newStock, CancellationToken ct)
    {
        var summary = await reads.GetByIdAsync(offerId, ct);
        if (summary is null)
            searchEngine.Remove(offerId);
        else
            searchEngine.Index(summary with { Stock = newStock });
    }
}
