namespace FlashSales.Application.Ports;

/// <summary>Driven port: runs a unit of work inside a single database transaction.</summary>
public interface IUnitOfWork
{
    Task<TResult> WithinTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> work, CancellationToken ct);
}
