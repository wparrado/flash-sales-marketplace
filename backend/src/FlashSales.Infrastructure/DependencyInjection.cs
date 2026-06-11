using FlashSales.Application.Contracts;
using FlashSales.Application.Ports;
using FlashSales.Application.UseCases.ProcessOrder;
using FlashSales.Application.UseCases.ProcessOrder.Steps;
using FlashSales.Infrastructure.Caching;
using FlashSales.Infrastructure.Outbox;
using FlashSales.Infrastructure.Payments;
using FlashSales.Infrastructure.Persistence;
using FlashSales.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace FlashSales.Infrastructure;

/// <summary>Composition of every adapter against its port. The only place that knows both sides.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<FlashSalesDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton(TimeProvider.System);

        // Persistence adapters (write + read sides)
        services.AddScoped<OfferRepository>();
        services.AddScoped<IOfferRepository>(sp => sp.GetRequiredService<OfferRepository>());
        services.AddScoped<IStockAuthority>(sp => sp.GetRequiredService<OfferRepository>());
        services.AddScoped<IStockReader>(sp => sp.GetRequiredService<OfferRepository>());
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOfferReadRepository, OfferReadRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // Inventory cache (cache-aside over IMemoryCache)
        services.AddMemoryCache();
        services.AddScoped<IInventoryCache, InMemoryInventoryCache>();

        // Search: in-memory fuzzy engine, hydrated at startup.
        // Swapping to Elasticsearch == replacing these registrations.
        services.AddSingleton<ISearchEngine, LevenshteinSearchEngine>();
        services.AddHostedService<SearchIndexInitializer>();

        // Transactional outbox: events persist with the business change; the
        // dispatcher projects them onto the read models (cache + search index).
        // A future broker relay replaces only the dispatcher's fan-out.
        services.AddScoped<IOutbox, EfOutbox>();
        services.AddScoped<OutboxProcessor>();
        services.AddHostedService<OutboxDispatcher>();

        // Payments: simulated provider wrapped in the Polly resilience pipeline.
        services.AddSingleton(PaymentResilienceOptions.Default);
        services.AddSingleton<ResiliencePipeline<PaymentResult>>(sp =>
            ResilientPaymentGateway.CreatePipeline(sp.GetRequiredService<PaymentResilienceOptions>()));
        services.AddScoped<SimulatedPaymentGateway>();
        services.AddScoped<IPaymentGateway>(sp => new ResilientPaymentGateway(
            sp.GetRequiredService<SimulatedPaymentGateway>(),
            sp.GetRequiredService<ResiliencePipeline<PaymentResult>>()));

        // Checkout validation chain — registration order IS the chain order.
        services.AddScoped<IOrderValidationStep, StockCacheCheckStep>();
        services.AddScoped<IOrderValidationStep, FraudRuleStep>();
        services.AddScoped<IOrderValidationStep, CouponStep>();

        services.AddScoped<ProcessOrderHandler>(sp => new ProcessOrderHandler(
            sp.GetServices<IOrderValidationStep>().ToList(),
            sp.GetRequiredService<IOfferRepository>(),
            sp.GetRequiredService<IStockAuthority>(),
            sp.GetRequiredService<IOrderRepository>(),
            sp.GetRequiredService<IPaymentGateway>(),
            sp.GetRequiredService<IOutbox>(),
            sp.GetRequiredService<IUnitOfWork>(),
            sp.GetRequiredService<TimeProvider>()));

        return services;
    }
}
