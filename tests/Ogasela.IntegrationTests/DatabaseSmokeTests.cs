using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Ogasela.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Ogasela.IntegrationTests;

public class DatabaseSmokeTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("ogasela")
        .WithUsername("ogasela")
        .WithPassword("ogasela")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task OgaselaDbContext_CanConnectAndApplyMigrations()
    {
        var options = new DbContextOptionsBuilder<OgaselaDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var dbContext = new OgaselaDbContext(options);

        var canConnect = await dbContext.Database.CanConnectAsync();
        canConnect.Should().BeTrue();

        await dbContext.Database.MigrateAsync();

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        pendingMigrations.Should().BeEmpty();
    }
}
