using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace FlashSales.Api.Extensions;

public static class OpenApiExtensions
{
    public static IServiceCollection AddFlashSalesOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "FLASH//MKT API",
                    Version = "v1",
                    Description = "Flash sales marketplace — catalog browsing, fuzzy search, and JWT-protected checkout."
                };

                document.Components = new OpenApiComponents
                {
                    SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                    {
                        ["Bearer"] = new OpenApiSecurityScheme
                        {
                            Type = SecuritySchemeType.Http,
                            Scheme = JwtBearerDefaults.AuthenticationScheme,
                            BearerFormat = "JWT",
                            Description = "Paste the JWT token obtained from `POST /api/auth/login`."
                        }
                    }
                };

                return Task.CompletedTask;
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
