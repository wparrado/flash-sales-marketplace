using FlashSales.Domain.Shared;

namespace FlashSales.Application.UseCases.ProcessOrder.Steps;

/// <summary>
/// Chain link 3: validates the coupon and enriches the context with the
/// discount rate consumed later by the pure pricing core.
/// </summary>
public sealed class CouponStep : IOrderValidationStep
{
    public static readonly Error InvalidCoupon =
        new("coupon.invalid", "The provided coupon code is not valid for this purchase.");

    private static readonly IReadOnlyDictionary<string, decimal> ActiveCoupons =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["FLASH10"] = 0.10m,
            ["VIP20"] = 0.20m
        };

    public Task<Result<OrderContext>> HandleAsync(OrderContext context, CancellationToken ct) =>
        Task.FromResult(context.Command.CouponCode switch
        {
            null or "" => Result<OrderContext>.Success(context),
            var code when ActiveCoupons.TryGetValue(code, out var rate) =>
                Result<OrderContext>.Success(context with { DiscountRate = rate }),
            _ => Result<OrderContext>.Failure(InvalidCoupon)
        });
}
