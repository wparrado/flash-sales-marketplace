using FlashSales.Application.Contracts;
using FlashSales.Application.Ports;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace FlashSales.Infrastructure.Payments;

public sealed record PaymentResilienceOptions(
    int MaxRetryAttempts,
    TimeSpan RetryBaseDelay,
    TimeSpan AttemptTimeout,
    double CircuitFailureRatio,
    int CircuitMinimumThroughput,
    TimeSpan CircuitSamplingDuration,
    TimeSpan CircuitBreakDuration)
{
    public static PaymentResilienceOptions Default => new(
        MaxRetryAttempts: 3,
        RetryBaseDelay: TimeSpan.FromMilliseconds(200),
        AttemptTimeout: TimeSpan.FromSeconds(2),
        CircuitFailureRatio: 0.5,
        CircuitMinimumThroughput: 5,
        CircuitSamplingDuration: TimeSpan.FromSeconds(10),
        CircuitBreakDuration: TimeSpan.FromSeconds(15));
}

/// <summary>
/// Decorator that wraps any IPaymentGateway with a Polly v8 resilience pipeline:
///
///   retry (exponential backoff + jitter, transient-only)
///     → circuit breaker (degrade gracefully when the provider is down)
///       → timeout (strict per-attempt budget)
///
/// Exhausted policies surface as a failed PaymentResult instead of an exception,
/// so the checkout use case compensates (restores stock) through its normal path.
/// </summary>
public sealed class ResilientPaymentGateway(
    IPaymentGateway inner,
    ResiliencePipeline<PaymentResult> pipeline) : IPaymentGateway
{
    public const string ProviderUnavailable = "payment_provider_unavailable";

    public async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct)
    {
        try
        {
            return await pipeline.ExecuteAsync(
                async token => await inner.ChargeAsync(request, token), ct);
        }
        catch (BrokenCircuitException)
        {
            return Unavailable();
        }
        catch (TimeoutRejectedException)
        {
            return Unavailable();
        }
        catch (HttpRequestException)
        {
            return Unavailable();
        }
    }

    private static PaymentResult Unavailable() =>
        new(Succeeded: false, Reference: null, FailureReason: ProviderUnavailable, IsTransient: true);

    public static ResiliencePipeline<PaymentResult> CreatePipeline(PaymentResilienceOptions options)
    {
        var transientFailures = new PredicateBuilder<PaymentResult>()
            .Handle<HttpRequestException>()
            .Handle<TimeoutRejectedException>()
            .HandleResult(result => !result.Succeeded && result.IsTransient);

        return new ResiliencePipelineBuilder<PaymentResult>()
            .AddRetry(new RetryStrategyOptions<PaymentResult>
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true, // decorrelates retry storms across concurrent checkouts
                Delay = options.RetryBaseDelay,
                ShouldHandle = transientFailures
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<PaymentResult>
            {
                FailureRatio = options.CircuitFailureRatio,
                MinimumThroughput = options.CircuitMinimumThroughput,
                SamplingDuration = options.CircuitSamplingDuration,
                BreakDuration = options.CircuitBreakDuration,
                ShouldHandle = transientFailures
            })
            .AddTimeout(options.AttemptTimeout)
            .Build();
    }
}
