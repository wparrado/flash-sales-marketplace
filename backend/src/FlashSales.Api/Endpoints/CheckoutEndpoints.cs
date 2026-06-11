using System.Security.Claims;
using FlashSales.Api.Contracts;
using FlashSales.Api.Extensions;
using FlashSales.Api.Middleware;
using FlashSales.Application.UseCases.ProcessOrder;
using FlashSales.SharedKernel;

namespace FlashSales.Api.Endpoints;

/// <summary>
/// Write side (CQRS): the critical billing path. Protected by JWT and isolated
/// behind its own concurrency bulkhead.
/// </summary>
public static class CheckoutEndpoints
{
    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        var checkout = app.MapGroup("/api/checkout")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.CheckoutPolicy);

        checkout.MapPost("/orders", async (
            CheckoutRequest request,
            ProcessOrderHandler handler,
            ClaimsPrincipal user,
            HttpContext http,
            CancellationToken ct) =>
        {
            var buyerId = user.BuyerId();
            if (buyerId is null)
                return Results.Unauthorized();

            var command = new ProcessOrderCommand(
                request.OfferId, buyerId.Value, request.Quantity,
                request.PaymentMethod, request.CouponCode,
                IdempotencyKey: http.Request.Headers["Idempotency-Key"].FirstOrDefault());

            var result = await handler.HandleAsync(command, ct);

            return result.Match(
                onSuccess: confirmation => Results.Created(
                    $"/api/checkout/orders/{confirmation.OrderId}",
                    new OrderResponse(
                        confirmation.OrderId,
                        confirmation.Status.ToString(),
                        confirmation.Total,
                        confirmation.Currency,
                        confirmation.PaymentReference)),
                onFailure: error => error.ToHttpResult(http.CorrelationId()));
        })
        .WithTags("Checkout")
        .WithSummary("Place a flash-sale order")
        .WithDescription("""
            Processes a purchase through the full validation chain:
            cache fast-fail → fraud rules → coupon → atomic stock reservation → payment → confirm or compensate.

            Supply an `Idempotency-Key` header to make retries safe — the handler returns the recorded outcome for duplicate keys.
            """)
        .Produces<OrderResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status402PaymentRequired)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
        .Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests);

        return app;
    }

    private static Guid? BuyerId(this ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? user.FindFirstValue("sub");
        return Guid.TryParse(subject, out var id) ? id : null;
    }

    /// <summary>Single mapping from domain error codes to HTTP semantics.</summary>
    private static IResult ToHttpResult(this Error error, string correlationId)
    {
        var statusCode = error.Code switch
        {
            "offer.not_found" => StatusCodes.Status404NotFound,
            "offer.out_of_stock" => StatusCodes.Status409Conflict,
            "offer.not_active" => StatusCodes.Status410Gone,
            "payment.rejected" => StatusCodes.Status402PaymentRequired,
            _ => StatusCodes.Status422UnprocessableEntity
        };

        return Results.Json(
            new ErrorResponse(error.Code, error.Message, correlationId),
            statusCode: statusCode);
    }
}
