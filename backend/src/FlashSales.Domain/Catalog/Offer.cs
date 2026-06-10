using FlashSales.Domain.Shared;

namespace FlashSales.Domain.Catalog;

/// <summary>
/// Flash-sale offer aggregate (write model). Immutable: state transitions
/// return new instances via Result, never mutate in place.
/// </summary>
public sealed record Offer(
    Guid Id,
    Guid SellerId,
    string Name,
    string Description,
    Money Price,
    int Stock,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt)
{
    public static class Errors
    {
        public static readonly Error InvalidQuantity =
            new("offer.invalid_quantity", "Quantity must be a positive integer.");

        public static Error OutOfStock(Guid offerId) =>
            new("offer.out_of_stock", $"Offer {offerId} does not have enough stock.");

        public static Error NotActive(Guid offerId) =>
            new("offer.not_active", $"Offer {offerId} is outside its flash-sale window.");
    }

    public bool IsActiveAt(DateTimeOffset instant) => instant >= StartsAt && instant < EndsAt;

    public Result<Offer> DecrementStock(int quantity) => quantity switch
    {
        <= 0 => Result<Offer>.Failure(Errors.InvalidQuantity),
        _ when quantity > Stock => Result<Offer>.Failure(Errors.OutOfStock(Id)),
        _ => Result<Offer>.Success(this with { Stock = Stock - quantity })
    };
}
