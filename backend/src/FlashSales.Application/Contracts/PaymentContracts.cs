using FlashSales.Domain.Shared;

namespace FlashSales.Application.Contracts;

public sealed record PaymentRequest(Guid OrderId, Money Amount, string Method, Guid BuyerId);

public sealed record PaymentResult(
    bool Succeeded,
    string? Reference,
    string? FailureReason,
    bool IsTransient);
