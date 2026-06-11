namespace FlashSales.Application.Ports;

/// <summary>
/// Driven port: the single authority over stock mutations. Kept separate from
/// IOfferRepository on purpose — this interface IS the API surface of a future
/// inventory microservice; extracting it means implementing this port over
/// HTTP/gRPC, nothing else.
/// </summary>
public interface IStockAuthority
{
    /// <summary>
    /// Atomically decrements stock if (and only if) enough is available.
    /// Returns the remaining stock, or null when the decrement was refused —
    /// the authoritative defense against overselling.
    /// </summary>
    Task<int?> TryDecrementStockAsync(Guid offerId, int quantity, CancellationToken ct);

    /// <summary>
    /// Compensating action when payment fails after a reservation.
    /// Returns the stock after restoration (consumed by the stock-changed event).
    /// </summary>
    Task<int> RestoreStockAsync(Guid offerId, int quantity, CancellationToken ct);
}
