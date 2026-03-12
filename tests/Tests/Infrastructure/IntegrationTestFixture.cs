using Alba;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Tests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
{
    public const string Name = "integration-tests";
}

public sealed class IntegrationTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("tododb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .WithCleanUp(true)
        .Build();

    public IAlbaHost Host { get; private set; } = null!;
    public IDocumentStore Store => Host.Services.GetRequiredService<IDocumentStore>();
    public string ConnectionString => _postgresContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        Environment.SetEnvironmentVariable("ConnectionStrings__tododb", ConnectionString);
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");

        Host = await AlbaHost.For<Program>();

        await Store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (Host is not null)
        {
            await Host.DisposeAsync();
        }

        await _postgresContainer.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        const string truncateSql = """
            DO $$
            DECLARE
                table_name text;
            BEGIN
                FOR table_name IN
                    SELECT tablename
                    FROM pg_tables
                    WHERE schemaname = 'public'
                LOOP
                    EXECUTE 'TRUNCATE TABLE public.' || quote_ident(table_name) || ' CASCADE';
                END LOOP;
            END $$;
            """;

        await using var command = new NpgsqlCommand(truncateSql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
