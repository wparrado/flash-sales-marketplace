using AwesomeAssertions;
using FlashSales.Domain.Shared;

namespace FlashSales.UnitTests.Domain;

public class MoneyTests
{
    [Fact]
    public void Add_SameCurrency_SumsAmounts()
    {
        var sum = Money.Usd(10.50m) + Money.Usd(4.25m);

        sum.Should().Be(Money.Usd(14.75m));
    }

    [Fact]
    public void Add_DifferentCurrency_Throws()
    {
        var act = () => Money.Usd(10m) + new Money(10m, "EUR");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MultiplyByQuantity_ScalesAmount()
    {
        (Money.Usd(19.99m) * 3).Should().Be(Money.Usd(59.97m));
    }

    [Fact]
    public void ApplyRate_RoundsToTwoDecimals_AwayFromZero()
    {
        // 33.335 * 0.10 = 3.3335 -> 3.33 ; midpoint 3.335 rounds to 3.34
        Money.Usd(33.35m).ApplyRate(0.10m).Should().Be(Money.Usd(3.34m));
    }

    [Fact]
    public void Zero_IsAdditiveIdentity()
    {
        (Money.Usd(5m) + Money.Zero("USD")).Should().Be(Money.Usd(5m));
    }

    [Fact]
    public void NegativeAmount_IsRejected()
    {
        var act = () => Money.Usd(-1m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
