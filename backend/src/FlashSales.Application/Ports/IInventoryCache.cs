namespace FlashSales.Application.Ports;

/// <summary>
/// Driven port: microsecond-latency stock lookups for the hot read path.
/// Advisory only — the database conditional update remains the source of truth.
/// </summary>
public interface IInventoryCache
{
    /// <summary>Returns the cached stock, or null when the key is unknown (cache miss).</summary>
    Task<int?> GetStockAsync(Guid offerId, CancellationToken ct);

    /// <summary>Write-through after a committed stock change.</summary>
    Task SetStockAsync(Guid offerId, int stock, CancellationToken ct);

    /// <summary>Drops the key so the next read repopulates from the database.</summary>
    Task InvalidateAsync(Guid offerId, CancellationToken ct);
}
