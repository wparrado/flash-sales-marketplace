using Testcontainers.PostgreSql;

namespace FlashSales.IntegrationTests;

/// <summary>
/// One PostgreSQL container shared by the whole test collection (singleton
/// container pattern): containers start once, tests stay fast.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("flashsales_tests")
        .WithUsername("flashsales")
        .WithPassword("flashsales")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
