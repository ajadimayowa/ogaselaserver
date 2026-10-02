using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ogasela.Application.AdIntegrations;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Infrastructure.Persistence;
using Ogasela.IntegrationTests.AdIntegrations;
using Ogasela.IntegrationTests.Accounts;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Ogasela.IntegrationTests.Admin;

/// <summary>
/// Real DI wiring throughout except IAdPlatformClient, swapped for the same stateful
/// FakeAdPlatformClient Phase 10's tests use - EraseUserDataCommand's best-effort platform-side
/// revoke must never make a real network call in a test.
/// </summary>
public sealed class AdminApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public readonly FakeAdPlatformClient FacebookClient = new(AdPlatform.Facebook);

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

        // Publishing goes live immediately in these tests; the moderator-approval step is
        // covered on its own by ListingApprovalTests (ApprovalListingsApiFactory).
        builder.UseSetting("Listings:RequireApproval", "false");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISmsSender>();
            services.AddSingleton<ISmsSender, NoOpSmsSender>();

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender, NoOpEmailSender>();

            services.RemoveAll<IAdPlatformClient>();
            services.AddSingleton<IAdPlatformClient>(FacebookClient);
            services.AddSingleton<IAdPlatformClient>(new FakeAdPlatformClient(AdPlatform.TikTok));
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
