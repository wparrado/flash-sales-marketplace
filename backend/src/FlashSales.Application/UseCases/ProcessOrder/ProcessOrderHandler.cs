using FlashSales.Application.Contracts;
using FlashSales.Application.Ports;
using FlashSales.Domain.Catalog;
using FlashSales.Domain.Ordering;
using FlashSales.Domain.Ordering.Commissions;
using FlashSales.SharedKernel;

namespace FlashSales.Application.UseCases.ProcessOrder;

/// <summary>
/// Critical checkout use case. Shape:
///   validation chain (cache → fraud → coupon)
///   → load + activity check
///   → atomic stock reservation + pending order (one transaction)
///   → payment (outside the transaction)
///   → confirm, or compensate by restoring stock.
/// Composed over Result so each stage short-circuits without nested branching.
/// </summary>
public sealed class ProcessOrderHandler(
    IReadOnlyList<IOrderValidationStep> validationChain,
    IOfferRepository offers,
    IStockAuthority stock,
    IOrderRepository orders,
    IPaymentGateway payments,
    IInventoryCache cache,
    IOfferIndexUpdater indexUpdater,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    private const decimal TaxRate = 0.19m;

    public static class Errors
    {
        public static Error OfferNotFound(Guid offerId) =>
            new("offer.not_found", $"Offer {offerId} does not exist.");

        public static Error PaymentRejected(string? reason) =>
            new("payment.rejected", reason ?? "The payment was rejected.");
    }

    public async Task<Result<OrderConfirmation>> HandleAsync(
        ProcessOrderCommand command, CancellationToken ct)
    {
        var validated = await RunValidationChainAsync(new OrderContext(command), ct);
        if (validated.IsFailure)
            return Result<OrderConfirmation>.Failure(validated.Error);

        var offerResult = await LoadActiveOfferAsync(command.OfferId, ct);
        if (offerResult.IsFailure)
            return Result<OrderConfirmation>.Failure(offerResult.Error);

        var reservation = await ReserveStockAndPlaceOrderAsync(offerResult.Value, validated.Value, ct);
        if (reservation.IsFailure)
            return Result<OrderConfirmation>.Failure(reservation.Error);

        return await ChargeAndSettleAsync(offerResult.Value, reservation.Value, command, ct);
    }

    /// <summary>Chain of Responsibility folded over Result.Bind: first failure wins.</summary>
    private async Task<Result<OrderContext>> RunValidationChainAsync(
        OrderContext context, CancellationToken ct)
    {
        var current = Result<OrderContext>.Success(context);
        foreach (var step in validationChain)
            current = await current.BindAsync(ctx => step.HandleAsync(ctx, ct));
        return current;
    }

    private async Task<Result<Offer>> LoadActiveOfferAsync(Guid offerId, CancellationToken ct)
    {
        var offer = await offers.GetByIdAsync(offerId, ct);
        return offer switch
        {
            null => Result<Offer>.Failure(Errors.OfferNotFound(offerId)),
            _ when !offer.IsActiveAt(clock.GetUtcNow()) =>
                Result<Offer>.Failure(Offer.Errors.NotActive(offerId)),
            _ => Result<Offer>.Success(offer)
        };
    }

    private Task<Result<StockReservation>> ReserveStockAndPlaceOrderAsync(
        Offer offer, OrderContext context, CancellationToken ct) =>
        unitOfWork.WithinTransactionAsync(async innerCt =>
        {
            var stockAfter = await stock.TryDecrementStockAsync(
                offer.Id, context.Command.Quantity, innerCt);

            if (stockAfter is null)
                return Result<StockReservation>.Failure(Offer.Errors.OutOfStock(offer.Id));

            var order = PlaceOrder(offer, context);
            await orders.AddAsync(order, innerCt);
            return Result<StockReservation>.Success(new StockReservation(order, stockAfter.Value));
        }, ct);

    private Order PlaceOrder(Offer offer, OrderContext context)
    {
        var lines = new[] { new OrderLine(offer.Id, offer.Name, offer.Price, context.Command.Quantity) };
        var subtotal = offer.Price * context.Command.Quantity;

        return Order.Place(
            buyerId: context.Command.BuyerId,
            sellerId: offer.SellerId,
            lines: lines,
            taxRate: TaxRate,
            discount: subtotal.ApplyRate(context.DiscountRate),
            commissionStrategy: CommissionStrategySelector.ForTier(offer.SellerTier),
            placedAt: clock.GetUtcNow());
    }

    private async Task<Result<OrderConfirmation>> ChargeAndSettleAsync(
        Offer offer, StockReservation reservation, ProcessOrderCommand command, CancellationToken ct)
    {
        var order = reservation.Order;
        var payment = await payments.ChargeAsync(
            new PaymentRequest(order.Id, order.Totals.Total, command.PaymentMethod, order.BuyerId), ct);

        return payment.Succeeded
            ? await ConfirmAsync(offer, reservation, payment.Reference!, ct)
            : await CompensateAsync(offer, reservation, command, payment, ct);
    }

    /// <summary>
    /// Saga compensation: payment failed after stock was reserved, so the
    /// reservation is rolled back and the failed order is kept for audit.
    /// The cache entry is invalidated (not recomputed) — the next read
    /// repopulates from the authoritative database.
    /// </summary>
    private async Task<Result<OrderConfirmation>> CompensateAsync(
        Offer offer, StockReservation reservation, ProcessOrderCommand command,
        Contracts.PaymentResult payment, CancellationToken ct)
    {
        await stock.RestoreStockAsync(offer.Id, command.Quantity, ct);
        await cache.InvalidateAsync(offer.Id, ct);
        await orders.UpdateAsync(reservation.Order.Fail(payment.FailureReason ?? "payment.rejected"), ct);

        return Result<OrderConfirmation>.Failure(Errors.PaymentRejected(payment.FailureReason));
    }

    private async Task<Result<OrderConfirmation>> ConfirmAsync(
        Offer offer, StockReservation reservation, string paymentReference, CancellationToken ct)
    {
        var confirmed = reservation.Order.Confirm(paymentReference);
        await orders.UpdateAsync(confirmed, ct);
        await cache.SetStockAsync(offer.Id, reservation.StockAfter, ct);
        await indexUpdater.OfferStockChangedAsync(offer.Id, reservation.StockAfter, ct);

        return Result<OrderConfirmation>.Success(new OrderConfirmation(
            confirmed.Id,
            confirmed.Status,
            confirmed.Totals.Total.Amount,
            confirmed.Totals.Total.Currency,
            confirmed.PaymentReference));
    }

    private sealed record StockReservation(Order Order, int StockAfter);
}
