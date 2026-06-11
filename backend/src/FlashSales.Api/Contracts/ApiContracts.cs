namespace FlashSales.Api.Contracts;

// The API contract layer. These records ARE the contract mirrored by
// frontend/packages/app-kernel/src/api/types.ts — contract-shape integration tests pin them.

/// <summary>Credentials for obtaining a JWT token.</summary>
/// <param name="Username">Demo users: <c>demo</c> or <c>admin</c>.</param>
/// <param name="Password">Demo passwords: <c>demo123</c> or <c>admin123</c>.</param>
public sealed record LoginRequest(string Username, string Password);

/// <summary>JWT token issued after successful authentication.</summary>
/// <param name="Token">Bearer token to include in the <c>Authorization</c> header.</param>
/// <param name="Username">Authenticated username echoed back.</param>
/// <param name="ExpiresAt">UTC timestamp when the token expires.</param>
public sealed record LoginResponse(string Token, string Username, DateTimeOffset ExpiresAt);

/// <summary>Payload for placing a flash-sale order.</summary>
/// <param name="OfferId">ID of the offer to purchase.</param>
/// <param name="Quantity">Number of units to buy (must be ≥ 1).</param>
/// <param name="PaymentMethod">
/// Payment method to charge. Demo values: <c>CreditCard</c> (succeeds),
/// <c>DeclinedCard</c> (hard decline), <c>FlakyCard</c> (triggers Polly retry + circuit breaker).
/// </param>
/// <param name="CouponCode">Optional discount coupon. Demo values: <c>FLASH10</c>, <c>VIP20</c>.</param>
public sealed record CheckoutRequest(
    Guid OfferId,
    int Quantity,
    string PaymentMethod,
    string? CouponCode);

/// <summary>Confirmation of a successfully placed order.</summary>
/// <param name="OrderId">Unique order identifier.</param>
/// <param name="Status">Order status: <c>Confirmed</c> or <c>Compensated</c>.</param>
/// <param name="Total">Final charged amount after discounts and tax.</param>
/// <param name="Currency">ISO 4217 currency code (e.g. <c>USD</c>).</param>
/// <param name="PaymentReference">Provider payment reference, if the charge succeeded.</param>
public sealed record OrderResponse(
    Guid OrderId,
    string Status,
    decimal Total,
    string Currency,
    string? PaymentReference);

/// <summary>Current available stock for an offer.</summary>
/// <param name="OfferId">Offer identifier.</param>
/// <param name="Stock">Units remaining, or <c>null</c> if the offer is unknown.</param>
public sealed record StockResponse(Guid OfferId, int? Stock);

/// <summary>Structured error envelope returned on all non-2xx responses.</summary>
/// <param name="Code">Machine-readable error code (e.g. <c>offer.out_of_stock</c>).</param>
/// <param name="Message">Human-readable description of the error.</param>
/// <param name="CorrelationId">Request correlation ID for log tracing.</param>
public sealed record ErrorResponse(string Code, string Message, string CorrelationId);
