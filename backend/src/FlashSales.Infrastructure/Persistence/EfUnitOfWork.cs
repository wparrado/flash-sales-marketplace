using FlashSales.Application.Ports;
using Microsoft.EntityFrameworkCore;

namespace FlashSales.Infrastructure.Persistence;

/// <summary>
/// Runs a unit of work inside one database transaction, through the Npgsql
/// execution strategy so transient connection failures retry the whole unit.
/// </summary>
public sealed class EfUnitOfWork(FlashSalesDbContext db) : IUnitOfWork
{
    public async Task<TResult> WithinTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> work, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(ct, async innerCt =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(innerCt);
            var result = await work(innerCt);
            await transaction.CommitAsync(innerCt);
            return result;
        });
    }
}
