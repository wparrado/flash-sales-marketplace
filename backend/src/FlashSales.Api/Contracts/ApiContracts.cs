namespace FlashSales.Api.Contracts;

// The API contract layer. These records ARE the contract mirrored by
// frontend/packages/app-kernel/src/api/types.ts — contract-shape integration tests pin them.

public sealed record LoginRequest(string Username, string Password);

public sealed record LoginResponse(string Token, string Username, DateTimeOffset ExpiresAt);

public sealed record CheckoutRequest(
    Guid OfferId,
    int Quantity,
    string PaymentMethod,
    string? CouponCode);

public sealed record OrderResponse(
    Guid OrderId,
    string Status,
    decimal Total,
    string Currency,
    string? PaymentReference);

public sealed record StockResponse(Guid OfferId, int? Stock);

public sealed record ErrorResponse(string Code, string Message, string CorrelationId);
