using FlashSales.SharedKernel;

namespace FlashSales.Domain.Ordering;

/// <summary>One purchased offer within an order. Immutable.</summary>
public sealed record OrderLine(Guid OfferId, string OfferName, Money UnitPrice, int Quantity)
{
    public Money Subtotal => UnitPrice * Quantity;
}
