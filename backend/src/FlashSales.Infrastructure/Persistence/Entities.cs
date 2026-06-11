using FlashSales.Domain.Catalog;
using FlashSales.Domain.Ordering;
using FlashSales.Domain.Ordering.Commissions;
using FlashSales.SharedKernel;

namespace FlashSales.Infrastructure.Persistence;

/// <summary>
/// Persistence models, deliberately separate from domain records: the domain
/// stays persistence-ignorant and EF-friendly mutability stays out of it.
/// </summary>
public class OfferEntity
{
    public Guid Id { get; set; }
    public Guid SellerId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal PriceAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public int Stock { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public SellerTier SellerTier { get; set; }

    public Offer ToDomain() => new(
        Id, SellerId, Name, Description, new Money(PriceAmount, Currency),
        Stock, StartsAt, EndsAt, SellerTier);

    public static OfferEntity FromDomain(Offer offer) => new()
    {
        Id = offer.Id,
        SellerId = offer.SellerId,
        Name = offer.Name,
        Description = offer.Description,
        PriceAmount = offer.Price.Amount,
        Currency = offer.Price.Currency,
        Stock = offer.Stock,
        StartsAt = offer.StartsAt,
        EndsAt = offer.EndsAt,
        SellerTier = offer.SellerTier
    };
}

public class OrderEntity
{
    public Guid Id { get; set; }
    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }
    public OrderStatus Status { get; set; }
    public string? PaymentReference { get; set; }
    public string? FailureReason { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTimeOffset PlacedAt { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public decimal Commission { get; set; }
    public decimal SellerPayout { get; set; }
    public string Currency { get; set; } = "USD";
    public List<OrderLineEntity> Lines { get; set; } = [];

    public static OrderEntity FromDomain(Order order) => new()
    {
        Id = order.Id,
        BuyerId = order.BuyerId,
        SellerId = order.SellerId,
        Status = order.Status,
        PaymentReference = order.PaymentReference,
        FailureReason = order.FailureReason,
        IdempotencyKey = order.IdempotencyKey,
        PlacedAt = order.PlacedAt,
        Subtotal = order.Totals.Subtotal.Amount,
        Discount = order.Totals.Discount.Amount,
        Tax = order.Totals.Tax.Amount,
        Total = order.Totals.Total.Amount,
        Commission = order.Totals.Commission.Amount,
        SellerPayout = order.Totals.SellerPayout.Amount,
        Currency = order.Totals.Total.Currency,
        Lines = order.Lines.Select(OrderLineEntity.FromDomain).ToList()
    };

    public void ApplyStatus(Order order)
    {
        Status = order.Status;
        PaymentReference = order.PaymentReference;
        FailureReason = order.FailureReason;
    }

    /// <summary>Reconstructs the domain aggregate (used by idempotent replay lookups).</summary>
    public Order ToDomain() => new()
    {
        Id = Id,
        BuyerId = BuyerId,
        SellerId = SellerId,
        Lines = Lines
            .Select(l => new OrderLine(l.OfferId, l.OfferName, new Money(l.UnitPrice, Currency), l.Quantity))
            .ToList(),
        Totals = new OrderTotals(
            Subtotal: new Money(Subtotal, Currency),
            Discount: new Money(Discount, Currency),
            Tax: new Money(Tax, Currency),
            Total: new Money(Total, Currency),
            Commission: new Money(Commission, Currency),
            SellerPayout: new Money(SellerPayout, Currency)),
        Status = Status,
        PaymentReference = PaymentReference,
        FailureReason = FailureReason,
        IdempotencyKey = IdempotencyKey,
        PlacedAt = PlacedAt
    };
}

public class OrderLineEntity
{
    public long Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid OfferId { get; set; }
    public string OfferName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public static OrderLineEntity FromDomain(OrderLine line) => new()
    {
        OfferId = line.OfferId,
        OfferName = line.OfferName,
        UnitPrice = line.UnitPrice.Amount,
        Quantity = line.Quantity
    };
}
