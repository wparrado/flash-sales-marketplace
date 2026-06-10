using FlashSales.Api.Auth;
using FlashSales.Api.Contracts;
using FlashSales.Api.Middleware;

namespace FlashSales.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", (LoginRequest request, JwtTokenService tokens, HttpContext http) =>
        {
            var session = tokens.Authenticate(request.Username, request.Password);
            return session is { } s
                ? Results.Ok(new LoginResponse(s.Token, request.Username, s.ExpiresAt))
                : Results.Json(
                    new ErrorResponse(
                        "auth.invalid_credentials", "Invalid username or password.", http.CorrelationId()),
                    statusCode: StatusCodes.Status401Unauthorized);
        });

        return app;
    }
}
