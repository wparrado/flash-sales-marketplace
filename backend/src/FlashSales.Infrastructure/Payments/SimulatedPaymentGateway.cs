using FlashSales.Application.Contracts;
using FlashSales.Application.Ports;

namespace FlashSales.Infrastructure.Payments;

/// <summary>
/// Deterministic external-provider stand-in. Special payment methods let demos
/// and integration tests exercise every resilience path:
///   "DeclinedCard" → hard decline (no retry),
///   "FlakyCard"    → network failure (retry + circuit breaker),
///   anything else  → success.
/// </summary>
public sealed class SimulatedPaymentGateway : IPaymentGateway
{
    public async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(30), ct); // simulated provider latency

        return request.Method switch
        {
            "DeclinedCard" => new PaymentResult(false, null, "card_declined", IsTransient: false),
            "FlakyCard" => throw new HttpRequestException("simulated provider outage"),
            _ => new PaymentResult(true, $"pay_{Guid.NewGuid():N}", null, IsTransient: false)
        };
    }
}
