using FlashSales.Application.Ports;
using FlashSales.Domain.Ordering;
using Microsoft.EntityFrameworkCore;

namespace FlashSales.Infrastructure.Persistence;

public sealed class OrderRepository(FlashSalesDbContext db) : IOrderRepository
{
    public async Task AddAsync(Order order, CancellationToken ct)
    {
        db.Orders.Add(OrderEntity.FromDomain(order));
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Order order, CancellationToken ct)
    {
        var entity = await db.Orders.FirstAsync(o => o.Id == order.Id, ct);
        entity.ApplyStatus(order);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Order?> FindByIdempotencyKeyAsync(
        Guid buyerId, string idempotencyKey, CancellationToken ct)
    {
        var entity = await db.Orders.AsNoTracking()
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.BuyerId == buyerId && o.IdempotencyKey == idempotencyKey, ct);
        return entity?.ToDomain();
    }
}
