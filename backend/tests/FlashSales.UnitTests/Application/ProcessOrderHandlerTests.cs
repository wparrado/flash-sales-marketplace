using AwesomeAssertions;
using FlashSales.Application.UseCases.ProcessOrder;
using FlashSales.Application.UseCases.ProcessOrder.Steps;
using FlashSales.Domain.Catalog;
using FlashSales.Domain.Ordering;
using FlashSales.UnitTests.TestDoubles;
using Microsoft.Extensions.Time.Testing;

namespace FlashSales.UnitTests.Application;

/// <summary>
/// TDD test list for the critical checkout use case. Each test was written
/// red-first; the handler grew one behavior at a time.
/// </summary>
public class ProcessOrderHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-10T12:00:00Z");

    private readonly InMemoryOfferRepository _offers = new();
    private readonly InMemoryOrderRepository _orders = new();
    private readonly FakeInventoryCache _cache = new();
    private readonly FakePaymentGateway _payments = new();
    private readonly FakeOfferIndexUpdater _indexUpdater = new();
    private readonly FakeTimeProvider _clock = new(Now);

    private ProcessOrderHandler CreateHandler() => new(
        validationChain:
        [
            new StockCacheCheckStep(_cache),
            new FraudRuleStep(),
            new CouponStep()
        ],
        offers: _offers,
        stock: _offers,
        orders: _orders,
        payments: _payments,
        cache: _cache,
        indexUpdater: _indexUpdater,
        unitOfWork: new FakeUnitOfWork(),
        clock: _clock);

    private Offer SeedActiveOffer(int stock, decimal price = 100m)
    {
        var offer = new OfferBuilder()
            .WithStock(stock)
            .WithPrice(FlashSales.SharedKernel.Money.Usd(price))
            .WithWindow(Now.AddHours(-1), Now.AddHours(1))
            .Build();
        _offers.Seed(offer);
        _cache.Seed(offer.Id, stock);
        return offer;
    }

    private static ProcessOrderCommand Command(Offer offer, int quantity = 1, string? coupon = null) =>
        new(offer.Id, BuyerId: Guid.NewGuid(), Quantity: quantity, PaymentMethod: "CreditCard", CouponCode: coupon);

    [Fact]
    public async Task WithSufficientStock_CreatesConfirmedOrder()
    {
        var offer = SeedActiveOffer(stock: 10);

        var result = await CreateHandler().HandleAsync(Command(offer, quantity: 2), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(OrderStatus.Confirmed);
        result.Value.Total.Should().Be(238.00m, "2 x 100 + 19% tax");
        _offers.Find(offer.Id)!.Stock.Should().Be(8);
        _orders.All.Should().ContainSingle(o => o.Status == OrderStatus.Confirmed);
    }

    [Fact]
    public async Task WhenCacheReportsInsufficientStock_FailsFast_WithoutHittingRepository()
    {
        var offer = SeedActiveOffer(stock: 5);
        _cache.Seed(offer.Id, 0); // cache says sold out

        var result = await CreateHandler().HandleAsync(Command(offer, quantity: 1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("offer.out_of_stock");
        _offers.GetByIdCalls.Should().Be(0, "the cache check must short-circuit before any DB access");
        _payments.Charges.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenQuantityExceedsFraudThreshold_RejectsOrder()
    {
        var offer = SeedActiveOffer(stock: 100);

        var result = await CreateHandler().HandleAsync(Command(offer, quantity: 11), CancellationToken.None);

        result.Error.Code.Should().Be("order.fraud_suspected");
        _payments.Charges.Should().BeEmpty();
    }

    [Fact]
    public async Task WithValidCoupon_AppliesDiscountBeforeTaxAndCommission()
    {
        var offer = SeedActiveOffer(stock: 10, price: 100m);

        var result = await CreateHandler().HandleAsync(
            Command(offer, quantity: 1, coupon: "FLASH10"), CancellationToken.None);

        // 100 - 10% coupon = 90 net; +19% tax = 107.10
        result.Value.Total.Should().Be(107.10m);
        var order = _orders.All.Single();
        order.Totals.Discount.Amount.Should().Be(10m);
        order.Totals.Commission.Amount.Should().Be(9m, "10% standard commission over the 90 net");
    }

    [Fact]
    public async Task WithUnknownCoupon_FailsWithInvalidCoupon()
    {
        var offer = SeedActiveOffer(stock: 10);

        var result = await CreateHandler().HandleAsync(
            Command(offer, coupon: "BOGUS"), CancellationToken.None);

        result.Error.Code.Should().Be("coupon.invalid");
    }

    [Fact]
    public async Task WhenDatabaseDecrementRefuses_ReturnsOutOfStock_AndNeverCharges()
    {
        // Cache is stale (says 5) but the database has already sold out.
        var offer = SeedActiveOffer(stock: 0);
        _cache.Seed(offer.Id, 5);

        var result = await CreateHandler().HandleAsync(Command(offer, quantity: 1), CancellationToken.None);

        result.Error.Code.Should().Be("offer.out_of_stock");
        _payments.Charges.Should().BeEmpty("payment must only run after stock is reserved");
    }

    [Fact]
    public async Task WhenPaymentFails_RestoresStock_InvalidatesCache_AndPersistsFailedOrder()
    {
        var offer = SeedActiveOffer(stock: 10);
        _payments.NextResult = new(Succeeded: false, Reference: null,
            FailureReason: "card_declined", IsTransient: false);

        var result = await CreateHandler().HandleAsync(Command(offer, quantity: 2), CancellationToken.None);

        result.Error.Code.Should().Be("payment.rejected");
        _offers.Find(offer.Id)!.Stock.Should().Be(10, "reserved stock must be restored (compensation)");
        _cache.InvalidateCalls.Should().Contain(offer.Id);
        _orders.All.Should().ContainSingle(o => o.Status == OrderStatus.Failed);
    }

    [Fact]
    public async Task OnSuccess_UpdatesInventoryCache_AndNotifiesSearchIndex()
    {
        var offer = SeedActiveOffer(stock: 10);

        await CreateHandler().HandleAsync(Command(offer, quantity: 3), CancellationToken.None);

        _cache.SetCalls.Should().Contain((offer.Id, 7));
        _indexUpdater.Notifications.Should().Contain((offer.Id, 7));
    }

    [Fact]
    public async Task WhenOfferWindowExpired_RejectsAsNotActive()
    {
        var offer = new OfferBuilder()
            .WithStock(10)
            .WithWindow(Now.AddHours(-3), Now.AddHours(-1))
            .Build();
        _offers.Seed(offer);
        _cache.Seed(offer.Id, 10);

        var result = await CreateHandler().HandleAsync(Command(offer), CancellationToken.None);

        result.Error.Code.Should().Be("offer.not_active");
    }
}
