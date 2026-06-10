using FlashSales.Application.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlashSales.Infrastructure.Search;

/// <summary>
/// Hydrates the in-memory search index from the database at startup. With a
/// dedicated search engine this becomes an indexing pipeline; the port and the
/// rest of the system stay unchanged.
/// </summary>
public sealed class SearchIndexInitializer(
    IServiceScopeFactory scopeFactory,
    ISearchEngine searchEngine) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var reads = scope.ServiceProvider.GetRequiredService<IOfferReadRepository>();

        var offers = await reads.GetActiveAsync(limit: 10_000, cancellationToken);
        foreach (var offer in offers)
            searchEngine.Index(offer);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
