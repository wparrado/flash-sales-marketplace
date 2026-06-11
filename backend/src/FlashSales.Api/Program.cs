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

// Exposes the implicit Program class to WebApplicationFactory in integration tests.
public partial class Program;
