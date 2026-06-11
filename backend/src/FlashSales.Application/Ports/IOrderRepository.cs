using FlashSales.Domain.Ordering;

namespace FlashSales.Application.Ports;

/// <summary>Driven port: transactional store for orders.</summary>
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct);
    Task UpdateAsync(Order order, CancellationToken ct);

    /// <summary>Replay lookup: the recorded order for a buyer's idempotency key, if any.</summary>
    Task<Order?> FindByIdempotencyKeyAsync(Guid buyerId, string idempotencyKey, CancellationToken ct);
}
