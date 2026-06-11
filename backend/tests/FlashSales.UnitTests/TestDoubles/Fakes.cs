using System.Collections.Concurrent;
using FlashSales.Application.Contracts;
using FlashSales.Application.Ports;
using FlashSales.Domain.Catalog;
using FlashSales.Domain.Ordering;

namespace FlashSales.UnitTests.TestDoubles;

/// <summary>Hand-rolled fakes: real behavior over mock setups, per testing guidelines.</summary>
public sealed class InMemoryOfferRepository : IOfferRepository, IStockAuthority
{
    private readonly ConcurrentDictionary<Guid, Offer> _offers = new();

    public int GetByIdCalls;

    public void Seed(Offer offer) => _offers[offer.Id] = offer;

    public Offer? Find(Guid offerId) => _offers.GetValueOrDefault(offerId);

    public Task<Offer?> GetByIdAsync(Guid offerId, CancellationToken ct)
    {
        Interlocked.Increment(ref GetByIdCalls);
        return Task.FromResult(_offers.GetValueOrDefault(offerId));
    }

    public Task<int?> TryDecrementStockAsync(Guid offerId, int quantity, CancellationToken ct)
    {
        while (true)
        {
            if (!_offers.TryGetValue(offerId, out var current) || current.Stock < quantity)
                return Task.FromResult<int?>(null);

            var updated = current with { Stock = current.Stock - quantity };
            if (_offers.TryUpdate(offerId, updated, current))
                return Task.FromResult<int?>(updated.Stock);
        }
    }

    public Task<int> RestoreStockAsync(Guid offerId, int quantity, CancellationToken ct)
    {
        var restored = _offers.AddOrUpdate(offerId,
            _ => throw new InvalidOperationException("Cannot restore stock of unknown offer"),
            (_, current) => current with { Stock = current.Stock + quantity });
        return Task.FromResult(restored.Stock);
    }
}

public sealed class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public IReadOnlyCollection<Order> All => _orders.Values.ToList();

    public Task AddAsync(Order order, CancellationToken ct)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Order order, CancellationToken ct)
    {
        _orders[order.Id] = order;
        return Task.CompletedTask;
    }
}

public sealed class FakeInventoryCache : IInventoryCache
{
    private readonly ConcurrentDictionary<Guid, int> _stock = new();

    public List<(Guid OfferId, int Stock)> SetCalls { get; } = [];
    public List<Guid> InvalidateCalls { get; } = [];

    public void Seed(Guid offerId, int stock) => _stock[offerId] = stock;

    public Task<int?> GetStockAsync(Guid offerId, CancellationToken ct) =>
        Task.FromResult(_stock.TryGetValue(offerId, out var stock) ? stock : (int?)null);

    public Task SetStockAsync(Guid offerId, int stock, CancellationToken ct)
    {
        _stock[offerId] = stock;
        SetCalls.Add((offerId, stock));
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(Guid offerId, CancellationToken ct)
    {
        _stock.TryRemove(offerId, out _);
        InvalidateCalls.Add(offerId);
        return Task.CompletedTask;
    }
}

public sealed class FakePaymentGateway : IPaymentGateway
{
    public PaymentResult NextResult { get; set; } =
        new(Succeeded: true, Reference: "pay_ok", FailureReason: null, IsTransient: false);

    public List<PaymentRequest> Charges { get; } = [];

    public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct)
    {
        Charges.Add(request);
        return Task.FromResult(NextResult);
    }
}

public sealed class FakeOfferIndexUpdater : IOfferIndexUpdater
{
    public List<(Guid OfferId, int NewStock)> Notifications { get; } = [];

    public Task OfferStockChangedAsync(Guid offerId, int newStock, CancellationToken ct)
    {
        Notifications.Add((offerId, newStock));
        return Task.CompletedTask;
    }
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task<TResult> WithinTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> work, CancellationToken ct) => work(ct);
}
