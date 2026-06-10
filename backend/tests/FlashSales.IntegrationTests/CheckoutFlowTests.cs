using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AwesomeAssertions;
using FlashSales.Domain.Catalog;
using FlashSales.Domain.Shared;

namespace FlashSales.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class CheckoutFlowTests(PostgresFixture postgres) : IAsyncLifetime
{
    private FlashSalesApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new FlashSalesApiFactory(postgres.ConnectionString);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private static Offer NewOffer(int stock, decimal price = 100m) => new(
        Guid.NewGuid(), Guid.NewGuid(), $"Test Offer {Guid.NewGuid():N}",
        "Integration test flash sale", Money.Usd(price), stock,
        DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));

    private async Task<string> LoginAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "demo", password = "demo123" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginBody>();
        return body!.Token;
    }

    private async Task<HttpResponseMessage> CheckoutAsync(
        string token, Guid offerId, int quantity, string paymentMethod = "CreditCard")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/checkout/orders")
        {
            Content = JsonContent.Create(new
            {
                offerId, quantity, paymentMethod, couponCode = (string?)null
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _client.SendAsync(request);
    }

    private sealed record LoginBody(string Token, string Username, DateTimeOffset ExpiresAt);
    private sealed record OrderBody(Guid OrderId, string Status, decimal Total, string Currency, string? PaymentReference);

    [Fact]
    public async Task Checkout_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/checkout/orders",
            new { offerId = Guid.NewGuid(), quantity = 1, paymentMethod = "CreditCard" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "demo", password = "wrong" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullCheckout_ConfirmsOrder_DecrementsStock_AndCatalogReflectsIt()
    {
        var offer = await _factory.SeedOfferAsync(NewOffer(stock: 5));
        var token = await LoginAsync();

        var response = await CheckoutAsync(token, offer.Id, quantity: 2);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var order = await response.Content.ReadFromJsonAsync<OrderBody>();
        order!.Status.Should().Be("Confirmed");
        order.Total.Should().Be(238.00m, "2 x 100 + 19% tax");
        order.PaymentReference.Should().StartWith("pay_");

        (await _factory.ReadStockAsync(offer.Id)).Should().Be(3);

        var catalog = await _client.GetFromJsonAsync<CatalogOffer>($"/api/catalog/offers/{offer.Id}");
        catalog!.Stock.Should().Be(3);
    }

    private sealed record CatalogOffer(Guid Id, string Name, decimal Price, string Currency, int Stock);

    [Fact]
    public async Task ParallelCheckouts_NeverOversell_ExactlyStockManySucceed()
    {
        const int stock = 5;
        const int contenders = 20;
        var offer = await _factory.SeedOfferAsync(NewOffer(stock));
        var token = await LoginAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, contenders)
            .Select(_ => CheckoutAsync(token, offer.Id, quantity: 1)));

        var succeeded = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        var conflicted = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        succeeded.Should().Be(stock, "every unit must be sold exactly once — no overselling");
        conflicted.Should().Be(contenders - stock);
        (await _factory.ReadStockAsync(offer.Id)).Should().Be(0);
    }

    [Fact]
    public async Task DeclinedPayment_Returns402_AndRestoresStock()
    {
        var offer = await _factory.SeedOfferAsync(NewOffer(stock: 5));
        var token = await LoginAsync();

        var response = await CheckoutAsync(token, offer.Id, quantity: 1, paymentMethod: "DeclinedCard");

        response.StatusCode.Should().Be(HttpStatusCode.PaymentRequired);
        (await _factory.ReadStockAsync(offer.Id)).Should().Be(5, "compensation must restore the reserved unit");
    }

    [Fact]
    public async Task FuzzySearch_FindsOffer_WithLiveStock_AfterCheckoutUpdatesTheIndex()
    {
        var offer = await _factory.SeedOfferAsync(NewOffer(stock: 5) with { Name = "Quadrocopter Apex" });
        var token = await LoginAsync();

        // The checkout triggers the in-process index update for this offer.
        (await CheckoutAsync(token, offer.Id, quantity: 1))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        // Typo on purpose: "quadrocoptre" is 2 edits from "quadrocopter".
        var results = await _client.GetFromJsonAsync<List<CatalogOffer>>(
            "/api/catalog/search?q=quadrocoptre");

        results.Should().Contain(o => o.Id == offer.Id, "fuzzy matching must tolerate typos");
        results!.Single(o => o.Id == offer.Id).Stock
            .Should().Be(4, "the search read model carries the post-checkout stock");
    }

    [Fact]
    public async Task CorrelationId_IsEchoed_WhenProvided_AndGenerated_WhenAbsent()
    {
        var withHeader = new HttpRequestMessage(HttpMethod.Get, "/api/catalog/offers");
        withHeader.Headers.Add("X-Correlation-ID", "test-correlation-123");
        var echoed = await _client.SendAsync(withHeader);

        echoed.Headers.GetValues("X-Correlation-ID").Single()
            .Should().Be("test-correlation-123");

        var without = await _client.GetAsync("/api/catalog/offers");
        Guid.TryParse(without.Headers.GetValues("X-Correlation-ID").Single(), out _)
            .Should().BeTrue("a fresh GUID must be generated when the client sends none");
    }
}
