using FlashSales.Domain.Catalog;

namespace FlashSales.Application.Ports;

/// <summary>Driven port: transactional store for the Offer write model.</summary>
public interface IOfferRepository
{
    Task<Offer?> GetByIdAsync(Guid offerId, CancellationToken ct);

    /// <summary>
    /// Atomically decrements stock if (and only if) enough is available.
    /// Returns the remaining stock, or null when the decrement was refused —
    /// this is the single authoritative defense against overselling.
    /// </summary>
    Task<int?> TryDecrementStockAsync(Guid offerId, int quantity, CancellationToken ct);

    /// <summary>Compensating action used when payment fails after stock was reserved.</summary>
    Task RestoreStockAsync(Guid offerId, int quantity, CancellationToken ct);
}
