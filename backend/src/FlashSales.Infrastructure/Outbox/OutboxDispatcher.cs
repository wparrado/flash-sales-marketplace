using System.Text.Json;
using FlashSales.Application.Contracts;
using FlashSales.Application.Ports;
using FlashSales.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlashSales.Infrastructure.Outbox;

/// <summary>
/// Processes pending outbox messages: fans each event out to the read models
/// (inventory cache + search index). Extracted from the dispatcher loop so the
/// processing logic is testable and reusable by a future broker-publishing relay.
/// Single-instance by design; a multi-instance deployment adds
/// FOR UPDATE SKIP LOCKED here.
/// </summary>
public sealed class OutboxProcessor(
    FlashSalesDbContext db,
    IInventoryCache cache,
    ISearchEngine searchEngine,
    IOfferReadRepository reads,
    TimeProvider clock,
    ILogger<OutboxProcessor> logger)
{
    private const int BatchSize = 50;

    public async Task<int> ProcessPendingAsync(CancellationToken ct)
    {
        var batch = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.Id)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var message in batch)
        {
            await HandleAsync(message, ct);
            message.ProcessedAt = clock.GetUtcNow();
        }

        if (batch.Count > 0)
            await db.SaveChangesAsync(ct);

        return batch.Count;
    }

    private async Task HandleAsync(OutboxMessageEntity message, CancellationToken ct)
    {
        switch (message.EventType)
        {
            case nameof(OfferStockChanged):
                var stockChanged = JsonSerializer.Deserialize<OfferStockChanged>(message.Payload)!;
                await ApplyStockChangeAsync(stockChanged, ct);
                break;

            default:
                logger.LogWarning("Unknown outbox event type {EventType} (id {Id})",
                    message.EventType, message.Id);
                break;
        }
    }

    /// <summary>Read-model projection: cache + search index follow the committed stock.</summary>
    private async Task ApplyStockChangeAsync(OfferStockChanged stockChanged, CancellationToken ct)
    {
        await cache.SetStockAsync(stockChanged.OfferId, stockChanged.NewStock, ct);

        var summary = await reads.GetByIdAsync(stockChanged.OfferId, ct);
        if (summary is null)
            searchEngine.Remove(stockChanged.OfferId);
        else
            searchEngine.Index(summary with { Stock = stockChanged.NewStock });
    }
}

/// <summary>Polling relay: drains the outbox shortly after each commit.</summary>
public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(300);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
                await processor.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never let one bad batch kill the relay; messages stay pending.
                logger.LogError(ex, "Outbox dispatch iteration failed");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
