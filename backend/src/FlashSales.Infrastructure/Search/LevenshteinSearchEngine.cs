using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using FlashSales.Application.Contracts;
using FlashSales.Application.Ports;

namespace FlashSales.Infrastructure.Search;

/// <summary>
/// In-memory fuzzy search adapter: typo tolerance via Levenshtein distance over
/// normalized (lowercase, diacritic-free) tokens, with a prefix bonus so partial
/// terms like "nint" still match. Thread-safe through a lock-free dictionary;
/// queries scan pre-tokenized entries, so no allocation-heavy work per hit.
///
/// This adapter is intentionally simple: it is the swappable seam. A dedicated
/// engine (Elasticsearch, Meilisearch) replaces this class behind the same
/// <see cref="ISearchEngine"/> port without touching Domain or Application.
/// </summary>
public sealed class LevenshteinSearchEngine : ISearchEngine
{
    private sealed record IndexedOffer(OfferSummary Summary, string[] Tokens);

    private readonly ConcurrentDictionary<Guid, IndexedOffer> _entries = new();

    public void Index(OfferSummary offer) =>
        _entries[offer.Id] = new IndexedOffer(offer, Tokenize($"{offer.Name} {offer.Description}"));

    public void Remove(Guid offerId) => _entries.TryRemove(offerId, out _);

    public IReadOnlyList<OfferSummary> Search(string query, int maxDistance = 2, int limit = 20)
    {
        var queryTokens = Tokenize(query);
        if (queryTokens.Length == 0)
            return [];

        return _entries.Values
            .Select(entry => (entry.Summary, Score: Score(queryTokens, entry.Tokens, maxDistance)))
            .Where(scored => scored.Score is not null)
            .OrderBy(scored => scored.Score)
            .ThenBy(scored => scored.Summary.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(scored => scored.Summary)
            .ToList();
    }

    /// <summary>
    /// Sum of the best per-token distances, or null when any query token is
    /// beyond <paramref name="maxDistance"/> from every indexed token.
    /// </summary>
    private static int? Score(string[] queryTokens, string[] offerTokens, int maxDistance)
    {
        var total = 0;
        foreach (var queryToken in queryTokens)
        {
            var best = offerTokens
                .Select(token => DistanceWithPrefixBonus(queryToken, token))
                .Min();

            if (best > maxDistance)
                return null;
            total += best;
        }

        return total;
    }

    private static int DistanceWithPrefixBonus(string query, string candidate) =>
        candidate.StartsWith(query, StringComparison.Ordinal)
            ? 0
            : Levenshtein(query, candidate);

    /// <summary>Classic two-row dynamic-programming Levenshtein, O(min) memory.</summary>
    private static int Levenshtein(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        Span<int> previous = stackalloc int[b.Length + 1];
        Span<int> current = stackalloc int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
            previous[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var substitutionCost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + substitutionCost);
            }
            current.CopyTo(previous);
        }

        return previous[b.Length];
    }

    private static string[] Tokenize(string text) =>
        RemoveDiacritics(text.ToLowerInvariant())
            .Split([' ', ',', '.', '-', '_', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length > 1)
            .ToArray();

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var rune in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(rune) != UnicodeCategory.NonSpacingMark)
                builder.Append(rune);

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
