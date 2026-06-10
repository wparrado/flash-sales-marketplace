namespace FlashSales.Application.Contracts;

/// <summary>
/// CQRS read model for catalog browsing and search. Deliberately flat and
/// denormalized — optimized for serving, decoupled from the Offer write model.
/// </summary>
public sealed record OfferSummary(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int Stock,
    DateTimeOffset EndsAt);
