using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Infrastructure.Persistence;
using Ogasela.IntegrationTests.Accounts;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Ogasela.IntegrationTests.Verification;

/// <summary>
/// Runs the full API against real Postgres/Redis testcontainers. The Development environment
/// (set below) makes DI select the "Mock" verification provider and in-memory image storage
/// automatically (see appsettings.Development.json / Infrastructure.DependencyInjection), so
/// these tests never touch real AWS - exactly what lets them exercise every decision branch
/// through deterministic file-name flags recognised by MockFaceVerificationProvider.
/// </summary>
public sealed class VerificationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("ogasela")
        .WithUsername("ogasela")
        .WithPassword("ogasela")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISmsSender>();
            services.AddSingleton<ISmsSender, NoOpSmsSender>();

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, NoOpEmailSender>();
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}
