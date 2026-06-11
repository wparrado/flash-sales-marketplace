using System.Text.Json;
using FlashSales.Application.Ports;
using FlashSales.Infrastructure.Persistence;

namespace FlashSales.Infrastructure.Outbox;

/// <summary>
/// Transactional outbox adapter: the event row shares the caller's DbContext,
/// so it joins whatever transaction the unit of work opened — atomic with the
/// business change it describes.
/// </summary>
public sealed class EfOutbox(FlashSalesDbContext db, TimeProvider clock) : IOutbox
{
    public async Task EnqueueAsync<TEvent>(TEvent domainEvent, CancellationToken ct)
        where TEvent : class
    {
        db.OutboxMessages.Add(new OutboxMessageEntity
        {
            EventType = typeof(TEvent).Name,
            Payload = JsonSerializer.Serialize(domainEvent),
            OccurredAt = clock.GetUtcNow()
        });
        await db.SaveChangesAsync(ct);
    }
}
