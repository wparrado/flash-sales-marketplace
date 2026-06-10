using System.Text;
using FlashSales.Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FlashSales.Api.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddJwtAuth(
        this IServiceCollection services, IConfiguration configuration)
    {
        var options = new JwtOptions(
            Key: configuration["Jwt:Key"]
                 ?? throw new InvalidOperationException("Jwt:Key is not configured"),
            Issuer: configuration["Jwt:Issuer"] ?? "FlashSales",
            Audience: configuration["Jwt:Audience"] ?? "FlashSales.Web");

        services.AddSingleton(options);
        services.AddSingleton<JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt => jwt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = options.Issuer,
                ValidateAudience = true,
                ValidAudience = options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),
                ClockSkew = TimeSpan.FromSeconds(30)
            });

        services.AddAuthorization();
        return services;
    }
}
