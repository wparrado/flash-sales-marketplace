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
                Results.Ok(await reads.GetActiveAsync(Math.Clamp(limit, 1, 200), ct)))
            .WithTags("Catalog")
            .WithSummary("List active flash-sale offers")
            .WithDescription("Returns all currently active offers (started and not yet ended). Results are served from a read-optimised projection; no write-side aggregates are loaded.")
            .Produces(StatusCodes.Status200OK)
;

        catalog.MapGet("/offers/{offerId:guid}",
            async (Guid offerId, IOfferReadRepository reads, HttpContext http, CancellationToken ct) =>
                await reads.GetByIdAsync(offerId, ct) is { } offer
                    ? Results.Ok(offer)
                    : Results.NotFound(new ErrorResponse(
                        "offer.not_found", $"Offer {offerId} does not exist.", http.CorrelationId())))
            .WithTags("Catalog")
            .WithSummary("Get a single offer by ID")
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
;

        catalog.MapGet("/offers/{offerId:guid}/stock",
            async (Guid offerId, IInventoryCache cache, CancellationToken ct) =>
                Results.Ok(new StockResponse(offerId, await cache.GetStockAsync(offerId, ct))))
            .WithTags("Catalog")
            .WithSummary("Get real-time stock for an offer")
            .WithDescription("Served from process-memory read-through cache. Suitable for high-frequency polling on the product page.")
            .Produces<StockResponse>(StatusCodes.Status200OK)
;

        catalog.MapGet("/search",
            (string q, ISearchEngine search, int limit = 20) =>
                Results.Ok(search.Search(q, maxDistance: 2, limit: Math.Clamp(limit, 1, 100))))
            .WithTags("Catalog")
            .WithSummary("Fuzzy-search offers by name")
            .WithDescription("In-memory Levenshtein search (max edit distance 2) over normalised, diacritic-free tokens. Example: `?q=nintnedo` returns \"Nintendo Switch 2\".")
            .Produces(StatusCodes.Status200OK)
;

        return app;
    }
}
