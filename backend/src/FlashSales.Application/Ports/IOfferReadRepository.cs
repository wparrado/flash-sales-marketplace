using FlashSales.Application.Contracts;

namespace FlashSales.Application.Ports;

/// <summary>
/// Driven port for the CQRS read side: denormalized, no-tracking projections
/// for catalog browsing. Kept apart from IOfferRepository (write model, strict
/// consistency) so each side can scale and be replaced independently.
/// </summary>
public interface IOfferReadRepository
{
    Task<IReadOnlyList<OfferSummary>> GetActiveAsync(int limit, CancellationToken ct);
    Task<OfferSummary?> GetByIdAsync(Guid offerId, CancellationToken ct);
}
