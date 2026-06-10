using AwesomeAssertions;
using FlashSales.Domain.Ordering;
using FlashSales.Domain.Ordering.Commissions;
using FlashSales.Domain.Shared;

namespace FlashSales.UnitTests.Domain;

public class OrderTests
{
    private static Order PlaceSampleOrder() => Order.Place(
        buyerId: Guid.NewGuid(),
        sellerId: Guid.NewGuid(),
        lines: [new OrderLine(Guid.NewGuid(), "Mechanical Keyboard", Money.Usd(80m), 2)],
        taxRate: 0.10m,
        discount: Money.Zero("USD"),
        commissionStrategy: new StandardCommissionStrategy(),
        placedAt: DateTimeOffset.Parse("2026-06-10T10:00:00Z"));

    [Fact]
    public void Place_ComputesTotals_AndStartsPendingPayment()
    {
        var order = PlaceSampleOrder();

        order.Status.Should().Be(OrderStatus.PendingPayment);
        order.Totals.Subtotal.Should().Be(Money.Usd(160m));
        order.Totals.Total.Should().Be(Money.Usd(176m));
        order.Totals.Commission.Should().Be(Money.Usd(16m));
    }

    [Fact]
    public void Confirm_TransitionsToConfirmed_WithPaymentReference()
    {
        var confirmed = PlaceSampleOrder().Confirm("pay_123");

        confirmed.Status.Should().Be(OrderStatus.Confirmed);
        confirmed.PaymentReference.Should().Be("pay_123");
    }

    [Fact]
    public void Fail_TransitionsToFailed_WithReason()
    {
        var failed = PlaceSampleOrder().Fail("payment.unavailable");

        failed.Status.Should().Be(OrderStatus.Failed);
        failed.FailureReason.Should().Be("payment.unavailable");
    }
}
