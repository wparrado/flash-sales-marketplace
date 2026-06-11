using FlashSales.Domain.Ordering.Commissions;
using FlashSales.SharedKernel;

namespace FlashSales.Domain.Ordering;

/// <summary>Pure, immutable snapshot of every monetary figure of an order.</summary>
public sealed record OrderTotals(
    Money Subtotal,
    Money Discount,
    Money Tax,
    Money Total,
    Money Commission,
    Money SellerPayout);

/// <summary>
/// Pure functional core of the checkout: no IO, no mutation, a single LINQ
/// aggregation plus derived values. Every figure is computed exactly once,
/// keeping cyclomatic complexity at 1.
/// </summary>
public static class OrderPricing
{
    public static OrderTotals Calculate(
        IReadOnlyCollection<OrderLine> lines,
        decimal taxRate,
        Money discount,
        ICommissionStrategy commissionStrategy)
    {
        var currency = lines.First().UnitPrice.Currency;
        var subtotal = lines.Aggregate(Money.Zero(currency), (acc, line) => acc + line.Subtotal);

        var effectiveDiscount = Min(discount, subtotal);
        var netAmount = subtotal - effectiveDiscount;
        var tax = netAmount.ApplyRate(taxRate);
        var commission = commissionStrategy.Calculate(netAmount);

        return new OrderTotals(
            Subtotal: subtotal,
            Discount: effectiveDiscount,
            Tax: tax,
            Total: netAmount + tax,
            Commission: commission,
            SellerPayout: netAmount - commission);
    }

    private static Money Min(Money a, Money b) => a.Amount <= b.Amount ? a : b;
}
