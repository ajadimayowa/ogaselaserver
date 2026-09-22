using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Domain.Notifications;
using Ogasela.Infrastructure.Persistence;
using Ogasela.IntegrationTests.Accounts;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Ogasela.IntegrationTests.Reviews;

/// <summary>
/// Swaps the real Push/Email notification channels for fakes (no FCM/SMTP credentials exist in
/// tests), so NotificationDispatcher's channel fan-out and preference checks can be verified
/// purely by inspecting the Notification rows/records it produces.
/// </summary>
public sealed class ReviewsAndNotificationsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public readonly FakeNotificationChannel PushChannel = new(NotificationChannel.Push);
    public readonly FakeNotificationChannel EmailChannel = new(NotificationChannel.Email);

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

            services.RemoveAll<INotificationChannel>();
            services.AddSingleton<INotificationChannel>(PushChannel);
            services.AddSingleton<INotificationChannel>(EmailChannel);
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
