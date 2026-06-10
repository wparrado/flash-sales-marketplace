using FlashSales.Domain.Ordering;

namespace FlashSales.Application.Ports;

/// <summary>Driven port: transactional store for orders.</summary>
public interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct);
    Task UpdateAsync(Order order, CancellationToken ct);
}
