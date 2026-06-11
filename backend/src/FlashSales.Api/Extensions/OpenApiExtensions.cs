using System.Reflection;
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

                // Register Bearer JWT security scheme
                document.Components ??= new();
                if (document.Components is not null)
                {
                    // Create OpenApiSecurityScheme instance dynamically
                    var openApiAssembly = Assembly.Load("Microsoft.OpenApi");
                    var securitySchemeType = openApiAssembly?.GetType("Microsoft.OpenApi.Models.OpenApiSecurityScheme");

                    if (securitySchemeType != null)
                    {
                        dynamic scheme = Activator.CreateInstance(securitySchemeType)
                            ?? throw new InvalidOperationException("Failed to create OpenApiSecurityScheme");

                        // Set properties via reflection
                        var securitySchemeTypeEnum = openApiAssembly?.GetType("Microsoft.OpenApi.Models.SecuritySchemeType");
                        if (securitySchemeTypeEnum != null)
                        {
                            var httpValue = Enum.Parse(securitySchemeTypeEnum, "Http");
                            securitySchemeType.GetProperty("Type")?.SetValue(scheme, httpValue);
                        }

                        securitySchemeType.GetProperty("Scheme")?.SetValue(scheme, JwtBearerDefaults.AuthenticationScheme);
                        securitySchemeType.GetProperty("BearerFormat")?.SetValue(scheme, "JWT");
                        securitySchemeType.GetProperty("Description")?.SetValue(scheme, "Paste the JWT token obtained from `POST /api/auth/login`.");

                        // Add to SecuritySchemes dictionary
                        var securitySchemesProperty = document.Components.GetType().GetProperty("SecuritySchemes");
                        var securitySchemes = securitySchemesProperty?.GetValue(document.Components);
                        if (securitySchemes != null)
                        {
                            securitySchemes.GetType().GetMethod("Add")?.Invoke(securitySchemes, ["Bearer", scheme]);
                        }
                    }
                }

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
