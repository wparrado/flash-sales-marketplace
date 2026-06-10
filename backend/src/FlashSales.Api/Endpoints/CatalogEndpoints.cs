using FlashSales.Api.Contracts;
using FlashSales.Api.Middleware;
using FlashSales.Application.Ports;

namespace FlashSales.Api.Endpoints;

/// <summary>
/// Read side (CQRS): browsing and search never touch the write model. Stock
/// reads are served by the in-memory cache; search by the in-memory fuzzy index.
/// </summary>
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/api/catalog");

        catalog.MapGet("/offers",
            async (IOfferReadRepository reads, CancellationToken ct, int limit = 50) =>
                Results.Ok(await reads.GetActiveAsync(Math.Clamp(limit, 1, 200), ct)));

        catalog.MapGet("/offers/{offerId:guid}",
            async (Guid offerId, IOfferReadRepository reads, HttpContext http, CancellationToken ct) =>
                await reads.GetByIdAsync(offerId, ct) is { } offer
                    ? Results.Ok(offer)
                    : Results.NotFound(new ErrorResponse(
                        "offer.not_found", $"Offer {offerId} does not exist.", http.CorrelationId())));

        // Hot path: answered from process memory (read-through cache), built to
        // survive thousands of concurrent stock polls without touching Postgres.
        catalog.MapGet("/offers/{offerId:guid}/stock",
            async (Guid offerId, IInventoryCache cache, CancellationToken ct) =>
                Results.Ok(new StockResponse(offerId, await cache.GetStockAsync(offerId, ct))));

        catalog.MapGet("/search",
            (string q, ISearchEngine search, int limit = 20) =>
                Results.Ok(search.Search(q, maxDistance: 2, limit: Math.Clamp(limit, 1, 100))));

        return app;
    }
}
