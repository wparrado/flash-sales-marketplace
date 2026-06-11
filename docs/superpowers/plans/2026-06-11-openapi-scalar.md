# OpenAPI + Scalar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `Microsoft.AspNetCore.OpenApi` + Scalar UI to the FlashSales API, available only in Development, with full schema documentation (types, descriptions, HTTP codes) for all endpoints.

**Architecture:** A new `OpenApiExtensions.cs` follows the existing Extensions pattern. XML doc comments on `ApiContracts.cs` feed schema descriptions automatically. Endpoint annotations (`.WithTags`, `.WithSummary`, `.Produces<T>`) are added inline to each endpoint group file.

**Tech Stack:** `Microsoft.AspNetCore.OpenApi` (built into .NET 10), `Scalar.AspNetCore` (UI only), XML doc generation via `<GenerateDocumentationFile>`.

---

## File Map

| Action | File | Responsibility |
|---|---|---|
| Modify | `FlashSales.Api/FlashSales.Api.csproj` | Add packages + enable XML doc generation |
| Create | `FlashSales.Api/Extensions/OpenApiExtensions.cs` | Register OpenAPI service + serve spec + Scalar UI |
| Modify | `FlashSales.Api/Program.cs` | Wire `AddFlashSalesOpenApi` / `UseFlashSalesOpenApi` |
| Modify | `FlashSales.Api/Contracts/ApiContracts.cs` | XML doc on every record and property |
| Modify | `FlashSales.Api/Endpoints/AuthEndpoints.cs` | Tags, summary, Produces |
| Modify | `FlashSales.Api/Endpoints/CatalogEndpoints.cs` | Tags, summary, Produces |
| Modify | `FlashSales.Api/Endpoints/CheckoutEndpoints.cs` | Tags, summary, Produces |

---

### Task 1: Add NuGet packages and enable XML documentation

**Files:**
- Modify: `backend/src/FlashSales.Api/FlashSales.Api.csproj`

- [ ] **Step 1: Add `Scalar.AspNetCore` package and XML doc generation**

Replace the `<PropertyGroup>` block and add a new `<ItemGroup>` so the file reads:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <ProjectReference Include="..\FlashSales.Infrastructure\FlashSales.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.9" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.9">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.16.0" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.16.0" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.15.2" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.15.1" />
    <PackageReference Include="Scalar.AspNetCore" Version="2.5.2" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
  </PropertyGroup>

</Project>
```

> `NoWarn 1591` silences "missing XML comment" warnings for files not yet documented.

- [ ] **Step 2: Restore packages**

```bash
cd backend
dotnet restore src/FlashSales.Api/FlashSales.Api.csproj
```

Expected: no errors, `Scalar.AspNetCore` appears in restored packages.

- [ ] **Step 3: Build to confirm no breakage**

```bash
dotnet build src/FlashSales.Api/FlashSales.Api.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add backend/src/FlashSales.Api/FlashSales.Api.csproj
git commit -m "chore: add Scalar.AspNetCore package and enable XML doc generation"
```

---

### Task 2: Create `OpenApiExtensions.cs`

**Files:**
- Create: `backend/src/FlashSales.Api/Extensions/OpenApiExtensions.cs`

- [ ] **Step 1: Create the extension file**

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
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

                var bearer = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT",
                    Description = "Paste the JWT token obtained from `POST /api/auth/login`."
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes["Bearer"] = bearer;

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
```

- [ ] **Step 2: Build to confirm it compiles**

```bash
cd backend
dotnet build src/FlashSales.Api/FlashSales.Api.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add backend/src/FlashSales.Api/Extensions/OpenApiExtensions.cs
git commit -m "feat: add OpenApiExtensions with JWT security scheme and Scalar UI (dev only)"
```

---

### Task 3: Wire into `Program.cs`

**Files:**
- Modify: `backend/src/FlashSales.Api/Program.cs`

- [ ] **Step 1: Add `AddFlashSalesOpenApi` to service registration and `UseFlashSalesOpenApi` after middleware**

The complete `Program.cs` should read:

```csharp
using FlashSales.Api.Endpoints;
using FlashSales.Api.Extensions;
using FlashSales.Api.Middleware;
using FlashSales.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured"));

builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddCheckoutBulkhead(builder.Configuration);
builder.Services.AddFlashSalesOpenApi();

builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetValue("Cors:AllowedOrigin", "http://localhost:5173")!)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

var app = builder.Build();

app.UseCors();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseFlashSalesOpenApi();

app.MapAuthEndpoints();
app.MapCatalogEndpoints();
app.MapCheckoutEndpoints();

await app.InitializeDatabaseAsync();

app.Run();

public partial class Program;
```

- [ ] **Step 2: Build**

```bash
cd backend
dotnet build src/FlashSales.Api/FlashSales.Api.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add backend/src/FlashSales.Api/Program.cs
git commit -m "feat: wire AddFlashSalesOpenApi and UseFlashSalesOpenApi into Program"
```

---

### Task 4: Document contract types (`ApiContracts.cs`)

**Files:**
- Modify: `backend/src/FlashSales.Api/Contracts/ApiContracts.cs`

- [ ] **Step 1: Add XML doc comments to every record and property**

```csharp
namespace FlashSales.Api.Contracts;

// The API contract layer. These records ARE the contract mirrored by
// frontend/packages/app-kernel/src/api/types.ts — contract-shape integration tests pin them.

/// <summary>Credentials for obtaining a JWT token.</summary>
/// <param name="Username">Demo users: <c>demo</c> or <c>admin</c>.</param>
/// <param name="Password">Demo passwords: <c>demo123</c> or <c>admin123</c>.</param>
public sealed record LoginRequest(string Username, string Password);

/// <summary>JWT token issued after successful authentication.</summary>
/// <param name="Token">Bearer token to include in the <c>Authorization</c> header.</param>
/// <param name="Username">Authenticated username echoed back.</param>
/// <param name="ExpiresAt">UTC timestamp when the token expires.</param>
public sealed record LoginResponse(string Token, string Username, DateTimeOffset ExpiresAt);

/// <summary>Payload for placing a flash-sale order.</summary>
/// <param name="OfferId">ID of the offer to purchase.</param>
/// <param name="Quantity">Number of units to buy (must be ≥ 1).</param>
/// <param name="PaymentMethod">
/// Payment method to charge. Demo values: <c>CreditCard</c> (succeeds),
/// <c>DeclinedCard</c> (hard decline), <c>FlakyCard</c> (triggers Polly retry + circuit breaker).
/// </param>
/// <param name="CouponCode">Optional discount coupon. Demo values: <c>FLASH10</c>, <c>VIP20</c>.</param>
public sealed record CheckoutRequest(
    Guid OfferId,
    int Quantity,
    string PaymentMethod,
    string? CouponCode);

/// <summary>Confirmation of a successfully placed order.</summary>
/// <param name="OrderId">Unique order identifier.</param>
/// <param name="Status">Order status: <c>Confirmed</c> or <c>Compensated</c>.</param>
/// <param name="Total">Final charged amount after discounts and tax.</param>
/// <param name="Currency">ISO 4217 currency code (e.g. <c>USD</c>).</param>
/// <param name="PaymentReference">Provider payment reference, if the charge succeeded.</param>
public sealed record OrderResponse(
    Guid OrderId,
    string Status,
    decimal Total,
    string Currency,
    string? PaymentReference);

/// <summary>Current available stock for an offer.</summary>
/// <param name="OfferId">Offer identifier.</param>
/// <param name="Stock">Units remaining, or <c>null</c> if the offer is unknown.</param>
public sealed record StockResponse(Guid OfferId, int? Stock);

/// <summary>Structured error envelope returned on all non-2xx responses.</summary>
/// <param name="Code">Machine-readable error code (e.g. <c>offer.out_of_stock</c>).</param>
/// <param name="Message">Human-readable description of the error.</param>
/// <param name="CorrelationId">Request correlation ID for log tracing.</param>
public sealed record ErrorResponse(string Code, string Message, string CorrelationId);
```

- [ ] **Step 2: Build**

```bash
cd backend
dotnet build src/FlashSales.Api/FlashSales.Api.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add backend/src/FlashSales.Api/Contracts/ApiContracts.cs
git commit -m "docs: XML doc comments on all API contract records and properties"
```

---

### Task 5: Annotate `AuthEndpoints.cs`

**Files:**
- Modify: `backend/src/FlashSales.Api/Endpoints/AuthEndpoints.cs`

- [ ] **Step 1: Add OpenAPI metadata to the login endpoint**

```csharp
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
        })
        .WithTags("Auth")
        .WithSummary("Authenticate and obtain a JWT token")
        .WithDescription("Validates credentials and returns a short-lived JWT Bearer token. Pass the token in `Authorization: Bearer <token>` on protected endpoints.")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
        .WithOpenApi();

        return app;
    }
}
```

- [ ] **Step 2: Build**

```bash
cd backend
dotnet build src/FlashSales.Api/FlashSales.Api.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add backend/src/FlashSales.Api/Endpoints/AuthEndpoints.cs
git commit -m "docs: OpenAPI annotations on AuthEndpoints"
```

---

### Task 6: Annotate `CatalogEndpoints.cs`

**Files:**
- Modify: `backend/src/FlashSales.Api/Endpoints/CatalogEndpoints.cs`

- [ ] **Step 1: Add OpenAPI metadata to all four catalog endpoints**

```csharp
using FlashSales.Api.Contracts;
using FlashSales.Api.Middleware;
using FlashSales.Application.Contracts;
using FlashSales.Application.Ports;

namespace FlashSales.Api.Endpoints;

/// <summary>
/// Read side (CQRS): browsing and search never touch the write model. Stock
/// reads are served by the in-memory cache; search by the in-memory fuzzy index.
/// </summary>
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/api/catalog");

        catalog.MapGet("/offers",
            async (IOfferReadRepository reads, CancellationToken ct, int limit = 50) =>
                Results.Ok(await reads.GetActiveAsync(Math.Clamp(limit, 1, 200), ct)))
            .WithTags("Catalog")
            .WithSummary("List active flash-sale offers")
            .WithDescription("Returns all currently active offers (started and not yet ended). Results are served from a read-optimised projection; no write-side aggregates are loaded.")
            .Produces<IReadOnlyList<OfferSummary>>(StatusCodes.Status200OK)
            .WithOpenApi();

        catalog.MapGet("/offers/{offerId:guid}",
            async (Guid offerId, IOfferReadRepository reads, HttpContext http, CancellationToken ct) =>
                await reads.GetByIdAsync(offerId, ct) is { } offer
                    ? Results.Ok(offer)
                    : Results.NotFound(new ErrorResponse(
                        "offer.not_found", $"Offer {offerId} does not exist.", http.CorrelationId())))
            .WithTags("Catalog")
            .WithSummary("Get a single offer by ID")
            .Produces<OfferSummary>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
            .WithOpenApi();

        catalog.MapGet("/offers/{offerId:guid}/stock",
            async (Guid offerId, IInventoryCache cache, CancellationToken ct) =>
                Results.Ok(new StockResponse(offerId, await cache.GetStockAsync(offerId, ct))))
            .WithTags("Catalog")
            .WithSummary("Get real-time stock for an offer")
            .WithDescription("Served from process-memory read-through cache. Suitable for high-frequency polling on the product page.")
            .Produces<StockResponse>(StatusCodes.Status200OK)
            .WithOpenApi();

        catalog.MapGet("/search",
            (string q, ISearchEngine search, int limit = 20) =>
                Results.Ok(search.Search(q, maxDistance: 2, limit: Math.Clamp(limit, 1, 100))))
            .WithTags("Catalog")
            .WithSummary("Fuzzy-search offers by name")
            .WithDescription("In-memory Levenshtein search (max edit distance 2) over normalised, diacritic-free tokens. Example: `?q=nintnedo` returns \"Nintendo Switch 2\".")
            .Produces<IReadOnlyList<OfferSummary>>(StatusCodes.Status200OK)
            .WithOpenApi();

        return app;
    }
}
```

> **Note:** `OfferSummary` lives in `FlashSales.Application.Contracts`. Verify the namespace resolves — the existing `CatalogEndpoints.cs` already uses `IOfferReadRepository` which returns it, so the type is already in scope transitively. If the build fails with an unresolved type, add `using FlashSales.Application.Contracts;` explicitly.

- [ ] **Step 2: Build**

```bash
cd backend
dotnet build src/FlashSales.Api/FlashSales.Api.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add backend/src/FlashSales.Api/Endpoints/CatalogEndpoints.cs
git commit -m "docs: OpenAPI annotations on CatalogEndpoints"
```

---

### Task 7: Annotate `CheckoutEndpoints.cs`

**Files:**
- Modify: `backend/src/FlashSales.Api/Endpoints/CheckoutEndpoints.cs`

- [ ] **Step 1: Add OpenAPI metadata to the checkout endpoint**

```csharp
using System.Security.Claims;
using FlashSales.Api.Contracts;
using FlashSales.Api.Extensions;
using FlashSales.Api.Middleware;
using FlashSales.Application.UseCases.ProcessOrder;
using FlashSales.SharedKernel;
using Microsoft.OpenApi.Models;

namespace FlashSales.Api.Endpoints;

/// <summary>
/// Write side (CQRS): the critical billing path. Protected by JWT and isolated
/// behind its own concurrency bulkhead.
/// </summary>
public static class CheckoutEndpoints
{
    public static IEndpointRouteBuilder MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        var checkout = app.MapGroup("/api/checkout")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingExtensions.CheckoutPolicy);

        checkout.MapPost("/orders", async (
            CheckoutRequest request,
            ProcessOrderHandler handler,
            ClaimsPrincipal user,
            HttpContext http,
            CancellationToken ct) =>
        {
            var buyerId = user.BuyerId();
            if (buyerId is null)
                return Results.Unauthorized();

            var command = new ProcessOrderCommand(
                request.OfferId, buyerId.Value, request.Quantity,
                request.PaymentMethod, request.CouponCode,
                IdempotencyKey: http.Request.Headers["Idempotency-Key"].FirstOrDefault());

            var result = await handler.HandleAsync(command, ct);

            return result.Match(
                onSuccess: confirmation => Results.Created(
                    $"/api/checkout/orders/{confirmation.OrderId}",
                    new OrderResponse(
                        confirmation.OrderId,
                        confirmation.Status.ToString(),
                        confirmation.Total,
                        confirmation.Currency,
                        confirmation.PaymentReference)),
                onFailure: error => error.ToHttpResult(http.CorrelationId()));
        })
        .WithTags("Checkout")
        .WithSummary("Place a flash-sale order")
        .WithDescription("""
            Processes a purchase through the full validation chain:
            cache fast-fail → fraud rules → coupon → atomic stock reservation → payment → confirm or compensate.

            Supply an `Idempotency-Key` header to make retries safe — the handler returns the recorded outcome for duplicate keys.
            """)
        .Produces<OrderResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status402PaymentRequired)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ErrorResponse>(StatusCodes.Status410Gone)
        .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity)
        .Produces<ErrorResponse>(StatusCodes.Status429TooManyRequests)
        .WithOpenApi(op =>
        {
            op.Parameters.Add(new OpenApiParameter
            {
                Name = "Idempotency-Key",
                In = ParameterLocation.Header,
                Required = false,
                Description = "Client-generated UUID. Re-sending with the same key returns the original response without re-processing.",
                Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
            });

            op.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = []
            });

            return op;
        });

        return app;
    }

    private static Guid? BuyerId(this ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? user.FindFirstValue("sub");
        return Guid.TryParse(subject, out var id) ? id : null;
    }

    /// <summary>Single mapping from domain error codes to HTTP semantics.</summary>
    private static IResult ToHttpResult(this Error error, string correlationId)
    {
        var statusCode = error.Code switch
        {
            "offer.not_found" => StatusCodes.Status404NotFound,
            "offer.out_of_stock" => StatusCodes.Status409Conflict,
            "offer.not_active" => StatusCodes.Status410Gone,
            "payment.rejected" => StatusCodes.Status402PaymentRequired,
            _ => StatusCodes.Status422UnprocessableEntity
        };

        return Results.Json(
            new ErrorResponse(error.Code, error.Message, correlationId),
            statusCode: statusCode);
    }
}
```

- [ ] **Step 2: Build**

```bash
cd backend
dotnet build src/FlashSales.Api/FlashSales.Api.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add backend/src/FlashSales.Api/Endpoints/CheckoutEndpoints.cs
git commit -m "docs: OpenAPI annotations on CheckoutEndpoints with Idempotency-Key header and JWT security"
```

---

### Task 8: Smoke test in the browser

- [ ] **Step 1: Start the stack**

```bash
# Terminal 1 — ensure postgres is running
docker compose up -d postgres

# Terminal 2 — start the API
cd backend
dotnet run --project src/FlashSales.Api
```

- [ ] **Step 2: Verify the spec is served**

Open `http://localhost:5038/openapi/v1.json` — expect a JSON document with `"title": "FLASH//MKT API"`.

- [ ] **Step 3: Verify Scalar UI**

Open `http://localhost:5038/scalar/v1` — expect the Scalar interface with three tag groups: **Auth**, **Catalog**, **Checkout**.

- [ ] **Step 4: Verify JWT flow in Scalar**

1. Expand `POST /api/auth/login` → send `{ "username": "demo", "password": "demo123" }` → copy the `token` field from the response.
2. Click the lock icon / Authenticate → paste the token.
3. Expand `POST /api/checkout/orders` → send a valid request → expect `201 Created`.

- [ ] **Step 5: Verify doc is NOT served in Production**

```bash
ASPNETCORE_ENVIRONMENT=Production dotnet run --project src/FlashSales.Api &
sleep 3
curl -o /dev/null -s -w "%{http_code}" http://localhost:5038/openapi/v1.json
# Expected: 404
kill %1
```

- [ ] **Step 6: Final commit**

```bash
git add .
git commit -m "docs: smoke-tested OpenAPI + Scalar integration"
```
