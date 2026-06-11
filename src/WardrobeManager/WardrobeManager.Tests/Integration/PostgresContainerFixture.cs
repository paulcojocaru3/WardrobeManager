using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using WardrobeManager.Infrastructure.Persistance;

namespace WardrobeManager.Tests.Integration;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    // PostgreSqlBuilder's built-in defaults — used to build the in-network connection string.
    private const string Database = "postgres";
    private const string Username = "postgres";
    private const string Password = "postgres";
    private const string NetworkAlias = "wm-postgres";

    private static readonly string? DockerNetwork = Environment.GetEnvironmentVariable("TEST_DOCKER_NETWORK");

    private readonly PostgreSqlContainer _container;

    public PostgresContainerFixture()
    {
        var builder = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg17");

        if (!string.IsNullOrEmpty(DockerNetwork))
        {
            builder = builder
                .WithNetwork(DockerNetwork)
                .WithNetworkAliases(NetworkAlias);
        }

        _container = builder.Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(GetConnectionString(), o => o.UseVector())
            .Options;

        return new ApplicationDbContext(options);
    }

    private string GetConnectionString()
    {
        if (!string.IsNullOrEmpty(DockerNetwork))
        {
            return $"Host={NetworkAlias};Port=5432;Database={Database};Username={Username};Password={Password}";
        }

        return _container.GetConnectionString();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "postgres";
}
