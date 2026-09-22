using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Ogasela.IntegrationTests.Accounts;

public sealed class AccountsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
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

        // Program.cs reads ConnectionStrings straight off `builder.Configuration` before
        // `Build()` runs, so the override must land via `UseSetting` (applied while the
        // minimal-hosting `WebApplicationBuilder` is still being assembled) rather than
        // `ConfigureAppConfiguration`, which is wired in too late for those early reads.
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());

        // Never let tests hit the real Termii/Brevo APIs - swap in no-op senders.
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

        // Force the host to build now (rather than lazily on the first HTTP request) so we
        // can run migrations against the container before any test issues a request.
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
