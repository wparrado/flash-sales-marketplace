namespace FlashSales.Application.Contracts;

/// <summary>
/// Explicit, versionable domain events. Today they travel through the
/// transactional outbox to in-process read models; in a distributed deployment
/// the SAME records become the message-bus contracts — consumers are already
/// written against them.
/// </summary>
public sealed record OfferStockChanged(Guid OfferId, int NewStock, DateTimeOffset OccurredAt);
