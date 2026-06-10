using AwesomeAssertions;
using FlashSales.Domain.Ordering;
using FlashSales.Domain.Ordering.Commissions;
using FlashSales.Domain.Shared;

namespace FlashSales.UnitTests.Domain;

public class OrderPricingTests
{
    private static OrderLine Line(decimal unitPrice, int qty) =>
        new(Guid.NewGuid(), "item", Money.Usd(unitPrice), qty);

    [Fact]
    public void Subtotal_IsTheSumOfAllLineSubtotals()
    {
        var totals = OrderPricing.Calculate(
            [Line(10m, 2), Line(5.50m, 3)],
            taxRate: 0m,
            discount: Money.Zero("USD"),
            commissionStrategy: new StandardCommissionStrategy());

        totals.Subtotal.Should().Be(Money.Usd(36.50m));
    }

    [Fact]
    public void Tax_IsAppliedOnTheDiscountedBase()
    {
        var totals = OrderPricing.Calculate(
            [Line(100m, 1)],
            taxRate: 0.19m,
            discount: Money.Usd(20m),
            commissionStrategy: new StandardCommissionStrategy());

        totals.Tax.Should().Be(Money.Usd(15.20m), "19% of (100 - 20)");
        totals.Total.Should().Be(Money.Usd(95.20m), "80 net + 15.20 tax");
    }

    [Fact]
    public void StandardCommission_IsTenPercentOfTheNetAmount()
    {
        var totals = OrderPricing.Calculate(
            [Line(200m, 1)],
            taxRate: 0m,
            discount: Money.Zero("USD"),
            commissionStrategy: new StandardCommissionStrategy());

        totals.Commission.Should().Be(Money.Usd(20m));
        totals.SellerPayout.Should().Be(Money.Usd(180m));
    }

    [Fact]
    public void PremiumSellerCommission_IsFivePercentOfTheNetAmount()
    {
        var totals = OrderPricing.Calculate(
            [Line(200m, 1)],
            taxRate: 0m,
            discount: Money.Zero("USD"),
            commissionStrategy: new PremiumSellerCommissionStrategy());

        totals.Commission.Should().Be(Money.Usd(10m));
        totals.SellerPayout.Should().Be(Money.Usd(190m));
    }

    [Fact]
    public void Discount_LargerThanSubtotal_IsClampedToSubtotal()
    {
        var totals = OrderPricing.Calculate(
            [Line(10m, 1)],
            taxRate: 0.19m,
            discount: Money.Usd(50m),
            commissionStrategy: new StandardCommissionStrategy());

        totals.Discount.Should().Be(Money.Usd(10m));
        totals.Total.Should().Be(Money.Usd(0m));
    }

    [Fact]
    public void CommissionStrategies_AreSelectableBySellerTier()
    {
        CommissionStrategySelector.ForTier(SellerTier.Standard)
            .Should().BeOfType<StandardCommissionStrategy>();
        CommissionStrategySelector.ForTier(SellerTier.Premium)
            .Should().BeOfType<PremiumSellerCommissionStrategy>();
    }
}
