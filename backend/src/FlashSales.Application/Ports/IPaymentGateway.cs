using FlashSales.Application.Contracts;

namespace FlashSales.Application.Ports;

/// <summary>Driven port: external payment provider. Resilience policies live in the adapter.</summary>
public interface IPaymentGateway
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct);
}
