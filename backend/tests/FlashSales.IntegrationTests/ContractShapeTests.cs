using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using FlashSales.Domain.Catalog;
using FlashSales.Domain.Shared;

namespace FlashSales.IntegrationTests;

/// <summary>
/// Consumer-driven contract tests: these records are written from the
/// CONSUMER's perspective and mirror frontend/src/api/types.ts. If a backend
/// change reshapes a payload (rename, removal, type change), these break in CI
/// before the frontend ever sees a malformed response.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ContractShapeTests(PostgresFixture postgres) : IAsyncLifetime
{
    private FlashSalesApiFactory _factory = null!;
    private HttpClient _client = null!;

    // --- Contract mirror (keep in sync with frontend/src/api/types.ts) ---
    private sealed record OfferSummaryContract(
        Guid Id, string Name, string Description, decimal Price,
        string Currency, int Stock, DateTimeOffset EndsAt);

    private sealed record LoginResponseContract(string Token, string Username, DateTimeOffset ExpiresAt);

    private sealed record StockResponseContract(Guid OfferId, int? Stock);

    private sealed record ErrorResponseContract(string Code, string Message, string CorrelationId);
    // ----------------------------------------------------------------------

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

    private static Offer NewOffer() => new(
        Guid.NewGuid(), Guid.NewGuid(), "Contract Offer", "Contract test offer",
        Money.Usd(50m), 7, DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1));

    [Fact]
    public async Task CatalogOffer_Payload_MatchesTheConsumerContract()
    {
        var offer = await _factory.SeedOfferAsync(NewOffer());

        var payload = await _client.GetFromJsonAsync<OfferSummaryContract>(
            $"/api/catalog/offers/{offer.Id}");

        payload.Should().NotBeNull();
        payload!.Id.Should().Be(offer.Id);
        payload.Name.Should().NotBeNullOrWhiteSpace();
        payload.Currency.Should().Be("USD");
        payload.Stock.Should().Be(7);
        payload.EndsAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_Payload_MatchesTheConsumerContract()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { username = "demo", password = "demo123" });

        var payload = await response.Content.ReadFromJsonAsync<LoginResponseContract>();

        payload!.Token.Should().NotBeNullOrWhiteSpace();
        payload.Username.Should().Be("demo");
        payload.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task StockEndpoint_Payload_MatchesTheConsumerContract()
    {
        var offer = await _factory.SeedOfferAsync(NewOffer());

        var payload = await _client.GetFromJsonAsync<StockResponseContract>(
            $"/api/catalog/offers/{offer.Id}/stock");

        payload!.OfferId.Should().Be(offer.Id);
        payload.Stock.Should().Be(7);
    }

    [Fact]
    public async Task ErrorEnvelope_IsStable_AndAlwaysCarriesACorrelationId()
    {
        var response = await _client.GetAsync($"/api/catalog/offers/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponseContract>();

        payload!.Code.Should().Be("offer.not_found");
        payload.Message.Should().NotBeNullOrWhiteSpace();
        payload.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Payloads_UseCamelCase_AsTheFrontendExpects()
    {
        var offer = await _factory.SeedOfferAsync(NewOffer());

        var raw = await _client.GetStringAsync($"/api/catalog/offers/{offer.Id}");
        using var json = JsonDocument.Parse(raw);

        json.RootElement.TryGetProperty("name", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("endsAt", out _).Should().BeTrue();
        json.RootElement.TryGetProperty("Name", out _).Should().BeFalse("contract is camelCase");
    }
}
