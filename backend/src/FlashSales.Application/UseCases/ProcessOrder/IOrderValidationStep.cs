using FlashSales.SharedKernel;

namespace FlashSales.Application.UseCases.ProcessOrder;

/// <summary>
/// Immutable state flowing through the validation chain. Steps enrich it
/// (e.g. the coupon step sets the discount rate) by returning a new copy.
/// </summary>
public sealed record OrderContext(ProcessOrderCommand Command, decimal DiscountRate = 0m);

/// <summary>
/// Chain of Responsibility link. The chain is composed in DI order and folded
/// over Result.Bind: the first failing step short-circuits the whole pipeline.
/// </summary>
public interface IOrderValidationStep
{
    Task<Result<OrderContext>> HandleAsync(OrderContext context, CancellationToken ct);
}
