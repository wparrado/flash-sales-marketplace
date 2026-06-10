using AwesomeAssertions;
using FlashSales.Application.Contracts;
using FlashSales.Infrastructure.Search;

namespace FlashSales.UnitTests.Infrastructure;

public class LevenshteinSearchEngineTests
{
    private static OfferSummary Summary(string name, int stock = 10) => new(
        Id: Guid.NewGuid(),
        Name: name,
        Description: $"Great deal on {name}",
        Price: 99.99m,
        Currency: "USD",
        Stock: stock,
        EndsAt: DateTimeOffset.UtcNow.AddHours(1));

    private static LevenshteinSearchEngine EngineWith(params OfferSummary[] offers)
    {
        var engine = new LevenshteinSearchEngine();
        foreach (var offer in offers) engine.Index(offer);
        return engine;
    }

    [Fact]
    public void Search_ExactMatch_RanksFirst()
    {
        var engine = EngineWith(
            Summary("Nintendo Switch"),
            Summary("Nintendo Switch Pro Controller"),
            Summary("PlayStation 5"));

        var results = engine.Search("nintendo switch");

        results.Should().NotBeEmpty();
        results[0].Name.Should().Be("Nintendo Switch");
    }

    [Fact]
    public void Search_ToleratesTypos_WithinTwoEdits()
    {
        var engine = EngineWith(Summary("Nintendo Switch"));

        engine.Search("nintnedo").Should().ContainSingle(o => o.Name == "Nintendo Switch");
        engine.Search("swithc").Should().ContainSingle(o => o.Name == "Nintendo Switch");
    }

    [Fact]
    public void Search_ExcludesResults_BeyondMaxDistance()
    {
        var engine = EngineWith(Summary("Nintendo Switch"));

        engine.Search("playstation").Should().BeEmpty();
    }

    [Fact]
    public void Search_IsCaseAndDiacriticInsensitive()
    {
        var engine = EngineWith(Summary("Cámara Réflex Canon"));

        engine.Search("CAMARA").Should().ContainSingle();
        engine.Search("reflex").Should().ContainSingle();
    }

    [Fact]
    public void Index_SameOfferId_UpsertsTheEntry()
    {
        var offer = Summary("Drone DJI Mini", stock: 5);
        var engine = EngineWith(offer);

        engine.Index(offer with { Stock = 2 });

        engine.Search("drone").Single().Stock.Should().Be(2);
    }

    [Fact]
    public void Remove_DropsTheOffer_FromResults()
    {
        var offer = Summary("Drone DJI Mini");
        var engine = EngineWith(offer);

        engine.Remove(offer.Id);

        engine.Search("drone").Should().BeEmpty();
    }

    [Fact]
    public void Search_RespectsTheResultLimit()
    {
        var engine = EngineWith(Enumerable.Range(1, 30)
            .Select(i => Summary($"Gaming Mouse {i}")).ToArray());

        engine.Search("gaming", limit: 20).Should().HaveCount(20);
    }
}
