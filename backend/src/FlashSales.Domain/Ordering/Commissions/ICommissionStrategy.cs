using FlashSales.SharedKernel;

namespace FlashSales.Domain.Ordering.Commissions;

/// <summary>
/// Strategy pattern: marketplace commission varies per seller tier (and can grow
/// to per-category or negotiated rates) without touching the pricing pipeline.
/// </summary>
public interface ICommissionStrategy
{
    /// <summary>Commission charged by the marketplace over the net (discounted) amount.</summary>
    Money Calculate(Money netAmount);
}

public sealed class StandardCommissionStrategy : ICommissionStrategy
{
    private const decimal Rate = 0.10m;
    public Money Calculate(Money netAmount) => netAmount.ApplyRate(Rate);
}

public sealed class PremiumSellerCommissionStrategy : ICommissionStrategy
{
    private const decimal Rate = 0.05m;
    public Money Calculate(Money netAmount) => netAmount.ApplyRate(Rate);
}

public static class CommissionStrategySelector
{
    public static ICommissionStrategy ForTier(SellerTier tier) => tier switch
    {
        SellerTier.Premium => new PremiumSellerCommissionStrategy(),
        _ => new StandardCommissionStrategy()
    };
}
