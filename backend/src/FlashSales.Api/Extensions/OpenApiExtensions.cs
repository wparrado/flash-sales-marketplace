using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

namespace FlashSales.Api.Extensions;

public static class OpenApiExtensions
{
    public static IServiceCollection AddFlashSalesOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer(async (document, context, cancellationToken) =>
            {
                document.Info ??= new();
                document.Info.Title = "FLASH//MKT API";
                document.Info.Version = "v1";
                document.Info.Description = "Flash sales marketplace — catalog browsing, fuzzy search, and JWT-protected checkout.";

                await Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication UseFlashSalesOpenApi(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.MapOpenApi();                             // /openapi/v1.json
        app.MapScalarApiReference(options =>
        {
            options.Title = "FLASH//MKT API";
            options.Theme = ScalarTheme.DeepSpace;
        });                                           // /scalar/v1

        return app;
    }
}
