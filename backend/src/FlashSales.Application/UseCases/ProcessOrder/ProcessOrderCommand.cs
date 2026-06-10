namespace FlashSales.Application.UseCases.ProcessOrder;

public sealed record ProcessOrderCommand(
    Guid OfferId,
    Guid BuyerId,
    int Quantity,
    string PaymentMethod,
    string? CouponCode = null);

public sealed record OrderConfirmation(
    Guid OrderId,
    Domain.Ordering.OrderStatus Status,
    decimal Total,
    string Currency,
    string? PaymentReference);
