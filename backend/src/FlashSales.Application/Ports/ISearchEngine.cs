using FlashSales.Application.Contracts;

namespace FlashSales.Application.Ports;

/// <summary>
/// Driven port for catalog search. The in-memory Levenshtein adapter serves the
/// technical test; swapping to Elasticsearch/OpenSearch/Meilisearch is a new
/// adapter plus one DI registration — no domain or application change.
/// </summary>
public interface ISearchEngine
{
    /// <summary>Fuzzy search tolerating up to <paramref name="maxDistance"/> edits per term.</summary>
    IReadOnlyList<OfferSummary> Search(string query, int maxDistance = 2, int limit = 20);

    /// <summary>Adds or replaces an offer in the index.</summary>
    void Index(OfferSummary offer);

    void Remove(Guid offerId);
}
