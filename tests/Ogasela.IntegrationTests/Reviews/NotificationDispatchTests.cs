using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ogasela.Api.Contracts.Notifications;
using Ogasela.Application.Notifications;
using Ogasela.Application.Notifications.GetNotifications;
using Ogasela.Domain.Notifications;
using Ogasela.Infrastructure.Listings;
using Ogasela.Infrastructure.Persistence;
using Ogasela.Infrastructure.Promotions.Seed;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Reviews;

public class NotificationDispatchTests : IClassFixture<ReviewsAndNotificationsApiFactory>
{
    private readonly ReviewsAndNotificationsApiFactory _factory;

    public NotificationDispatchTests(ReviewsAndNotificationsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListingTransitioningToExpiringSoon_NotifiesTheSellerOnEveryEnabledChannel()
    {
        var sellerClient = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(sellerClient, CategorySeedData.ElectronicsId);

        await BackdateToExpiringSoonAndRunJobAsync(listing.Id);

        var notifications = await GetNotificationsAsync(sellerClient);

        notifications.Items.Should().Contain(n => n.Channel == NotificationChannel.Push && n.Type == NotificationTypes.ListingExpiringSoon);
        notifications.Items.Should().Contain(n => n.Channel == NotificationChannel.Email && n.Type == NotificationTypes.ListingExpiringSoon);
    }

    [Fact]
    public async Task AUserWithAChannelDisabled_DoesNotReceiveANotificationOnThatChannel()
    {
        var sellerClient = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(sellerClient, CategorySeedData.ElectronicsId);

        var preferencesResponse = await sellerClient.PutAsJsonAsync(
            "/api/v1/notifications/preferences",
            new UpdateNotificationPreferencesRequest(
            [
                new PreferenceUpdateRequest(NotificationChannel.Email, NotificationCategories.ListingLifecycle, false)
            ]));
        preferencesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await BackdateToExpiringSoonAndRunJobAsync(listing.Id);

        var notifications = await GetNotificationsAsync(sellerClient);

        notifications.Items.Should().Contain(
            n => n.Channel == NotificationChannel.Push && n.Type == NotificationTypes.ListingExpiringSoon,
            "Push was never disabled, so it must still fire");

        notifications.Items.Should().NotContain(
            n => n.Channel == NotificationChannel.Email && n.Type == NotificationTypes.ListingExpiringSoon,
            "Email was explicitly disabled for this category, so no row should exist for it at all");
    }

    private async Task BackdateToExpiringSoonAndRunJobAsync(Guid listingId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();

        var listing = await dbContext.Listings.FirstAsync(l => l.Id == listingId);
        // Within the job's 24h expiring-soon window, but not yet past ExpiresAt.
        dbContext.Entry(listing).Property(l => l.ExpiresAt).CurrentValue = DateTime.UtcNow.AddHours(12);
        await dbContext.SaveChangesAsync();

        var job = scope.ServiceProvider.GetRequiredService<ListingExpiringSoonJob>();
        await job.RunAsync();
    }

    private static async Task<PagedNotificationsResponse> GetNotificationsAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/notifications");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PagedNotificationsResponse>(JsonOptions))!;
    }
}
