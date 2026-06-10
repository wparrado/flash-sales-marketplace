using FlashSales.Application.Ports;
using Microsoft.Extensions.Caching.Memory;

namespace FlashSales.Infrastructure.Caching;

/// <summary>Read side of the cache-aside pattern: how stock reaches the cache on a miss.</summary>
public interface IStockReader
{
    Task<int?> ReadStockAsync(Guid offerId, CancellationToken ct);
}

/// <summary>
/// Inventory cache adapter (cache-aside + read-through). Stock reads are served
/// from process memory in microseconds; only a miss touches the database. The
/// short TTL bounds staleness for browsers, while checkout always revalidates
/// against the database via the atomic conditional decrement.
///
/// Swappable for Redis (shared cache across instances) behind the same
/// IInventoryCache port.
/// </summary>
public sealed class InMemoryInventoryCache(IMemoryCache cache, IStockReader stockReader) : IInventoryCache
{
    private static readonly TimeSpan StockTtl = TimeSpan.FromSeconds(5);

    private static string Key(Guid offerId) => $"stock:{offerId}";

    public async Task<int?> GetStockAsync(Guid offerId, CancellationToken ct)
    {
        if (cache.TryGetValue(Key(offerId), out int cachedStock))
            return cachedStock;

        var stock = await stockReader.ReadStockAsync(offerId, ct);
        if (stock is { } value)
            cache.Set(Key(offerId), value, StockTtl);

        return stock;
    }

    public Task SetStockAsync(Guid offerId, int stock, CancellationToken ct)
    {
        cache.Set(Key(offerId), stock, StockTtl);
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(Guid offerId, CancellationToken ct)
    {
        cache.Remove(Key(offerId));
        return Task.CompletedTask;
    }
}
