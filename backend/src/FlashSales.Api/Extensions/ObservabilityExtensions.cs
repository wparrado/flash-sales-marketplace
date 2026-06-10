using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace FlashSales.Api.Extensions;

public static class ObservabilityExtensions
{
    public const string ServiceName = "flashsales-api";

    /// <summary>
    /// Vendor-agnostic telemetry: traces, metrics and logs flow through the
    /// OpenTelemetry SDK and out via OTLP. Pointing the OTLP endpoint at
    /// Datadog, Grafana, Jaeger or New Relic is configuration, not code.
    /// IncludeScopes carries the CorrelationId logging scope into every record.
    /// </summary>
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var resource = ResourceBuilder.CreateDefault().AddService(ServiceName);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(ServiceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(ServiceName)
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.SetResourceBuilder(resource);
            logging.IncludeScopes = true;
            logging.AddOtlpExporter();
        });

        return builder;
    }
}
