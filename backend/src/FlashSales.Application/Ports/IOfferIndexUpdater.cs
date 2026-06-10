namespace FlashSales.Application.Ports;

/// <summary>
/// Driven port: propagates committed stock changes to read models (search index,
/// live stock badges). In-process today; an outbox + message bus in a distributed
/// deployment — the use case does not care.
/// </summary>
public interface IOfferIndexUpdater
{
    Task OfferStockChangedAsync(Guid offerId, int newStock, CancellationToken ct);
}
