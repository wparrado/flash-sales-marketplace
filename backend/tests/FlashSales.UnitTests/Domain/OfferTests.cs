using AwesomeAssertions;
using FlashSales.Domain.Catalog;
using FlashSales.Domain.Shared;
using FlashSales.UnitTests.TestDoubles;

namespace FlashSales.UnitTests.Domain;

public class OfferTests
{
    [Fact]
    public void DecrementStock_WithEnoughStock_ReturnsNewOfferWithReducedStock()
    {
        var offer = new OfferBuilder().WithStock(10).Build();

        var result = offer.DecrementStock(3);

        result.IsSuccess.Should().BeTrue();
        result.Value.Stock.Should().Be(7);
        offer.Stock.Should().Be(10, "the original offer is immutable");
    }

    [Fact]
    public void DecrementStock_WithInsufficientStock_FailsWithOutOfStock()
    {
        var offer = new OfferBuilder().WithStock(2).Build();

        var result = offer.DecrementStock(3);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("offer.out_of_stock");
    }

    [Fact]
    public void DecrementStock_WithNonPositiveQuantity_FailsWithInvalidQuantity()
    {
        var offer = new OfferBuilder().WithStock(5).Build();

        offer.DecrementStock(0).Error.Code.Should().Be("offer.invalid_quantity");
        offer.DecrementStock(-1).Error.Code.Should().Be("offer.invalid_quantity");
    }

    [Fact]
    public void IsActiveAt_InsideFlashWindow_IsTrue()
    {
        var now = DateTimeOffset.Parse("2026-06-10T12:00:00Z");
        var offer = new OfferBuilder()
            .WithWindow(now.AddHours(-1), now.AddHours(1))
            .Build();

        offer.IsActiveAt(now).Should().BeTrue();
        offer.IsActiveAt(now.AddHours(2)).Should().BeFalse();
        offer.IsActiveAt(now.AddHours(-2)).Should().BeFalse();
    }
}
