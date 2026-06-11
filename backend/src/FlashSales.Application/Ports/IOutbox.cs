namespace FlashSales.Application.Ports;

/// <summary>
/// Driven port: transactional outbox. Enqueued events are persisted within the
/// caller's ambient database transaction — they exist if and only if the
/// business change committed — and a dispatcher delivers them afterwards.
/// This removes the crash window between "stock committed" and "read models
/// updated", and is the seam where a message broker plugs in later.
/// </summary>
public interface IOutbox
{
    Task EnqueueAsync<TEvent>(TEvent domainEvent, CancellationToken ct) where TEvent : class;
}
