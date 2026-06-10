using AwesomeAssertions;
using FlashSales.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace FlashSales.UnitTests.Infrastructure;

public class InMemoryInventoryCacheTests
{
    private sealed class CountingStockReader : IStockReader
    {
        private readonly int? _stock;
        public int Reads;

        public CountingStockReader(int? stock) => _stock = stock;

        public Task<int?> ReadStockAsync(Guid offerId, CancellationToken ct)
        {
            Reads++;
            return Task.FromResult(_stock);
        }
    }

    private static InMemoryInventoryCache CreateCache(IStockReader reader) =>
        new(new MemoryCache(new MemoryCacheOptions()), reader);

    [Fact]
    public async Task GetStock_OnMiss_ReadsThrough_AndCachesTheValue()
    {
        var reader = new CountingStockReader(stock: 42);
        var cache = CreateCache(reader);
        var offerId = Guid.NewGuid();

        var first = await cache.GetStockAsync(offerId, CancellationToken.None);
        var second = await cache.GetStockAsync(offerId, CancellationToken.None);

        first.Should().Be(42);
        second.Should().Be(42);
        reader.Reads.Should().Be(1, "the second read must be served from memory");
    }

    [Fact]
    public async Task GetStock_WhenOfferUnknownInDatabase_ReturnsNull_WithoutCachingIt()
    {
        var reader = new CountingStockReader(stock: null);
        var cache = CreateCache(reader);
        var offerId = Guid.NewGuid();

        (await cache.GetStockAsync(offerId, CancellationToken.None)).Should().BeNull();
        (await cache.GetStockAsync(offerId, CancellationToken.None)).Should().BeNull();

        reader.Reads.Should().Be(2, "unknown offers must not be cached as zero stock");
    }

    [Fact]
    public async Task SetStock_WriteThrough_OverridesTheCachedValue()
    {
        var reader = new CountingStockReader(stock: 10);
        var cache = CreateCache(reader);
        var offerId = Guid.NewGuid();
        await cache.GetStockAsync(offerId, CancellationToken.None);

        await cache.SetStockAsync(offerId, 7, CancellationToken.None);

        (await cache.GetStockAsync(offerId, CancellationToken.None)).Should().Be(7);
        reader.Reads.Should().Be(1);
    }

    [Fact]
    public async Task Invalidate_ForcesTheNextRead_ToHitTheReader()
    {
        var reader = new CountingStockReader(stock: 10);
        var cache = CreateCache(reader);
        var offerId = Guid.NewGuid();
        await cache.GetStockAsync(offerId, CancellationToken.None);

        await cache.InvalidateAsync(offerId, CancellationToken.None);
        await cache.GetStockAsync(offerId, CancellationToken.None);

        reader.Reads.Should().Be(2);
    }
}
