using FlashSales.Domain.Shared;

namespace FlashSales.Application.UseCases.ProcessOrder.Steps;

/// <summary>
/// Chain link 2: cheap synchronous fraud heuristics. A real deployment would
/// call a risk-scoring service behind a port; the chain shape stays the same.
/// </summary>
public sealed class FraudRuleStep : IOrderValidationStep
{
    private const int MaxQuantityPerOrder = 10;

    public static readonly Error FraudSuspected =
        new("order.fraud_suspected", $"Orders above {MaxQuantityPerOrder} units per purchase are blocked.");

    public Task<Result<OrderContext>> HandleAsync(OrderContext context, CancellationToken ct) =>
        Task.FromResult(
            context.Command.Quantity > MaxQuantityPerOrder
                ? Result<OrderContext>.Failure(FraudSuspected)
                : Result<OrderContext>.Success(context));
}
