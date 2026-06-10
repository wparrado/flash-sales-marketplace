using AwesomeAssertions;
using FlashSales.Application.Contracts;
using FlashSales.Application.Ports;
using FlashSales.Domain.Shared;
using FlashSales.Infrastructure.Payments;

namespace FlashSales.UnitTests.Infrastructure;

public class ResilientPaymentGatewayTests
{
    private sealed class ScriptedGateway : IPaymentGateway
    {
        private readonly Func<int, Task<PaymentResult>> _script;
        public int Calls;

        public ScriptedGateway(Func<int, Task<PaymentResult>> script) => _script = script;

        public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct)
        {
            Calls++;
            return _script(Calls);
        }
    }

    private static readonly PaymentRequest SomeRequest =
        new(Guid.NewGuid(), Money.Usd(100m), "CreditCard", Guid.NewGuid());

    private static readonly PaymentResult Success =
        new(Succeeded: true, Reference: "pay_ok", FailureReason: null, IsTransient: false);

    private static PaymentResilienceOptions FastOptions(int circuitMinimumThroughput) => new(
        MaxRetryAttempts: 3,
        RetryBaseDelay: TimeSpan.FromMilliseconds(1),
        AttemptTimeout: TimeSpan.FromMilliseconds(100),
        CircuitFailureRatio: 0.5,
        CircuitMinimumThroughput: circuitMinimumThroughput,
        CircuitSamplingDuration: TimeSpan.FromSeconds(5),
        CircuitBreakDuration: TimeSpan.FromSeconds(5));

    /// <summary>High circuit threshold: the breaker stays out of the way of the test.</summary>
    private static ResilientPaymentGateway Decorate(IPaymentGateway inner) =>
        new(inner, ResilientPaymentGateway.CreatePipeline(FastOptions(circuitMinimumThroughput: 100)));

    /// <summary>Low circuit threshold: two failures are enough to open the breaker.</summary>
    private static ResilientPaymentGateway DecorateWithSensitiveCircuit(IPaymentGateway inner) =>
        new(inner, ResilientPaymentGateway.CreatePipeline(FastOptions(circuitMinimumThroughput: 2)));

    [Fact]
    public async Task RetriesTransientNetworkFailures_UntilSuccess()
    {
        var inner = new ScriptedGateway(call => call < 3
            ? throw new HttpRequestException("connection reset")
            : Task.FromResult(Success));

        var result = await Decorate(inner).ChargeAsync(SomeRequest, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        inner.Calls.Should().Be(3, "two transient failures then success");
    }

    [Fact]
    public async Task DoesNotRetry_NonTransientDeclines()
    {
        var declined = new PaymentResult(false, null, "card_declined", IsTransient: false);
        var inner = new ScriptedGateway(_ => Task.FromResult(declined));

        var result = await Decorate(inner).ChargeAsync(SomeRequest, CancellationToken.None);

        result.Should().Be(declined);
        inner.Calls.Should().Be(1, "a hard decline must not be retried");
    }

    [Fact]
    public async Task OpensCircuit_AfterRepeatedFailures_AndFailsFastWithoutCallingInner()
    {
        var inner = new ScriptedGateway(_ => throw new HttpRequestException("gateway down"));
        var gateway = DecorateWithSensitiveCircuit(inner);

        var firstResult = await gateway.ChargeAsync(SomeRequest, CancellationToken.None);
        var callsAfterFirst = inner.Calls;
        var secondResult = await gateway.ChargeAsync(SomeRequest, CancellationToken.None);

        firstResult.Succeeded.Should().BeFalse();
        secondResult.Succeeded.Should().BeFalse();
        secondResult.FailureReason.Should().Be("payment_provider_unavailable");
        inner.Calls.Should().Be(callsAfterFirst, "an open circuit must fail fast without calling the provider");
    }

    private sealed class SlowGateway : IPaymentGateway
    {
        // Honors the cancellation token, like a real HttpClient-based provider:
        // Polly v8 timeouts are cooperative.
        public async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return Success;
        }
    }

    [Fact]
    public async Task TimesOutSlowAttempts_AndDegradesGracefully()
    {
        var inner = new SlowGateway();

        var result = await Decorate(inner).ChargeAsync(SomeRequest, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.IsTransient.Should().BeTrue("a timeout is transient; the caller compensates and the user may retry");
    }
}
