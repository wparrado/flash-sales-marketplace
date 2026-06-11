using FlashSales.Domain.Catalog;

namespace FlashSales.Application.Ports;

/// <summary>Driven port: transactional store for the Offer write model.</summary>
public interface IOfferRepository
{
    Task<Offer?> GetByIdAsync(Guid offerId, CancellationToken ct);
}
