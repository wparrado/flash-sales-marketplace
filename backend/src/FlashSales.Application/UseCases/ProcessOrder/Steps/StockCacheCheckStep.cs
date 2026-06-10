using FlashSales.Application.Ports;
using FlashSales.Domain.Catalog;
using FlashSales.Domain.Shared;

namespace FlashSales.Application.UseCases.ProcessOrder.Steps;

/// <summary>
/// Chain link 1: rejects obviously hopeless purchases using the in-memory
/// stock cache, sparing the transactional database during sell-out spikes.
/// A cache miss passes through — the database stays authoritative.
/// </summary>
public sealed class StockCacheCheckStep(IInventoryCache cache) : IOrderValidationStep
{
    public async Task<Result<OrderContext>> HandleAsync(OrderContext context, CancellationToken ct)
    {
        var cachedStock = await cache.GetStockAsync(context.Command.OfferId, ct);

        return cachedStock is { } stock && stock < context.Command.Quantity
            ? Result<OrderContext>.Failure(Offer.Errors.OutOfStock(context.Command.OfferId))
            : Result<OrderContext>.Success(context);
    }
}
