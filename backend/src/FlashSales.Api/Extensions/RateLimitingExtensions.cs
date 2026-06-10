using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace FlashSales.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string CheckoutPolicy = "checkout";

    /// <summary>
    /// Bulkhead isolation at the entry adapter: checkout gets its own bounded
    /// concurrency budget. A flood on catalog/search endpoints cannot exhaust
    /// the resources that keep the marketplace billing, and a checkout overload
    /// degrades to fast 503s instead of queueing the whole server.
    /// (The payment dependency has its own inner bulkhead via Polly timeout +
    /// circuit breaker.)
    /// </summary>
    public static IServiceCollection AddCheckoutBulkhead(
        this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("Checkout:PermitLimit", 10);
        var queueLimit = configuration.GetValue("Checkout:QueueLimit", 20);

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status503ServiceUnavailable;
            limiter.AddConcurrencyLimiter(CheckoutPolicy, options =>
            {
                options.PermitLimit = permitLimit;
                options.QueueLimit = queueLimit;
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });
        });

        return services;
    }
}
