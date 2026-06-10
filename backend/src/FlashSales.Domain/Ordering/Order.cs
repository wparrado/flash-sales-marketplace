using FlashSales.Domain.Ordering.Commissions;
using FlashSales.Domain.Shared;

namespace FlashSales.Domain.Ordering;

public enum OrderStatus
{
    PendingPayment,
    Confirmed,
    Failed
}

/// <summary>
/// Order aggregate (write model). Created through <see cref="Place"/> so totals
/// are always computed by the pure pricing core; transitions return new instances.
/// </summary>
public sealed record Order
{
    public Guid Id { get; init; }
    public Guid BuyerId { get; init; }
    public Guid SellerId { get; init; }
    public IReadOnlyList<OrderLine> Lines { get; init; } = [];
    public OrderTotals Totals { get; init; } = null!;
    public OrderStatus Status { get; init; }
    public string? PaymentReference { get; init; }
    public string? FailureReason { get; init; }
    public DateTimeOffset PlacedAt { get; init; }

    public static Order Place(
        Guid buyerId,
        Guid sellerId,
        IReadOnlyList<OrderLine> lines,
        decimal taxRate,
        Money discount,
        ICommissionStrategy commissionStrategy,
        DateTimeOffset placedAt) => new()
    {
        Id = Guid.NewGuid(),
        BuyerId = buyerId,
        SellerId = sellerId,
        Lines = lines,
        Totals = OrderPricing.Calculate(lines, taxRate, discount, commissionStrategy),
        Status = OrderStatus.PendingPayment,
        PlacedAt = placedAt
    };

    public Order Confirm(string paymentReference) =>
        this with { Status = OrderStatus.Confirmed, PaymentReference = paymentReference };

    public Order Fail(string reason) =>
        this with { Status = OrderStatus.Failed, FailureReason = reason };
}
