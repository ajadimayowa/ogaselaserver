using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ogasela.Api.Contracts.AdIntegrations;
using Ogasela.Application.AdIntegrations;
using Ogasela.Application.AdIntegrations.ConnectAdAccount;
using Ogasela.Application.AdIntegrations.GetAdPerformance;
using Ogasela.Application.AdIntegrations.PromoteListing;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Infrastructure.AdIntegrations;
using Ogasela.Infrastructure.Persistence;
using Ogasela.Infrastructure.Promotions.Seed;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.AdIntegrations;

public class AdCampaignLifecycleTests : IClassFixture<AdIntegrationsApiFactory>
{
    private readonly AdIntegrationsApiFactory _factory;

    public AdCampaignLifecycleTests(AdIntegrationsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullLifecycle_ConnectPromotePollPauseResumeEnd_TransitionsCorrectly()
    {
        var client = await RegisterSellerAsync(_factory);
        var sellerProfileId = await GetSellerProfileIdAsync(client);

        var connectResponse = await client.PostAsync("/api/v1/integrations/facebook/connect", content: null);
        connectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var connected = await connectResponse.Content.ReadFromJsonAsync<ConnectAdAccountResponse>(JsonOptions);
        connected!.AuthorizationUrl.Should().NotBeNullOrEmpty();

        var callbackResponse = await _factory.CreateClient().GetAsync(
            $"/api/v1/integrations/facebook/callback?code=fake-auth-code&state={sellerProfileId}");
        callbackResponse.StatusCode.Should().Be(HttpStatusCode.OK, "the OAuth callback carries no bearer token, so it must succeed unauthenticated");

        var listing = await CreateDraftListingAsync(client, CategorySeedData.PhonesAndTabletsId, PromotionPlanSeedData.StandardId);

        var promoteRequest = new PromoteListingRequest(500_000m, 14, "Lagos, 18-35", "Great deal!", "Grab it before it's gone.");
        var promoteResponse = await client.PostAsJsonAsync($"/api/v1/listings/{listing.Id}/promote/facebook", promoteRequest);
        promoteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var campaign = await promoteResponse.Content.ReadFromJsonAsync<AdCampaignResponse>(JsonOptions);
        campaign!.Status.Should().Be(AdCampaignStatus.PendingReview);

        // Nothing has told the job the platform approved it yet - metrics stay empty until it does.
        await RunMetricsSyncJobAsync();
        var performanceBeforeApproval = await GetAdPerformanceAsync(client, listing.Id);
        performanceBeforeApproval.Campaigns.Should().ContainSingle(c => c.Status == AdCampaignStatus.PendingReview);

        // Simulate the platform approving the campaign, then let the job pick that up.
        _factory.FacebookClient.SetExternalStatus(
            campaign.ExternalCampaignId, AdCampaignStatus.Active, new AdMetricsSnapshot(1000, 40, 15_000m));
        await RunMetricsSyncJobAsync();

        var performanceAfterApproval = await GetAdPerformanceAsync(client, listing.Id);
        performanceAfterApproval.Campaigns.Should().ContainSingle(c => c.Platform == AdPlatform.Facebook && c.Status == AdCampaignStatus.Active);
        performanceAfterApproval.TotalImpressions.Should().Be(1000);
        performanceAfterApproval.TotalClicks.Should().Be(40);
        performanceAfterApproval.TotalSpendKobo.Should().Be(15_000m);

        var pauseResponse = await client.PostAsync($"/api/v1/listings/{listing.Id}/promote/facebook/pause", content: null);
        pauseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await pauseResponse.Content.ReadFromJsonAsync<AdCampaignResponse>(JsonOptions))!.Status.Should().Be(AdCampaignStatus.Paused);

        var resumeResponse = await client.PostAsync($"/api/v1/listings/{listing.Id}/promote/facebook/resume", content: null);
        resumeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resumeResponse.Content.ReadFromJsonAsync<AdCampaignResponse>(JsonOptions))!.Status.Should().Be(AdCampaignStatus.Active);

        var endResponse = await client.PostAsync($"/api/v1/listings/{listing.Id}/promote/facebook/end", content: null);
        endResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await endResponse.Content.ReadFromJsonAsync<AdCampaignResponse>(JsonOptions))!.Status.Should().Be(AdCampaignStatus.Ended);
    }

    [Fact]
    public async Task RevokedConnection_BlocksFurtherCampaignActions()
    {
        var client = await RegisterSellerAsync(_factory);
        var sellerProfileId = await GetSellerProfileIdAsync(client);

        await client.PostAsync("/api/v1/integrations/tiktok/connect", content: null);
        var callbackResponse = await _factory.CreateClient().GetAsync(
            $"/api/v1/integrations/tiktok/callback?code=fake-code&state={sellerProfileId}");
        callbackResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listing = await CreateDraftListingAsync(client, CategorySeedData.PhonesAndTabletsId, PromotionPlanSeedData.StandardId);
        var promoteRequest = new PromoteListingRequest(200_000m, 7, null, "Title", "Description");

        var promoteResponse = await client.PostAsJsonAsync($"/api/v1/listings/{listing.Id}/promote/tiktok", promoteRequest);
        promoteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var revokeResponse = await client.DeleteAsync("/api/v1/integrations/tiktok");
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var pauseAfterRevoke = await client.PostAsync($"/api/v1/listings/{listing.Id}/promote/tiktok/pause", content: null);
        pauseAfterRevoke.StatusCode.Should().Be(HttpStatusCode.Conflict, "a revoked connection must block further actions on an existing campaign");

        var anotherListing = await CreateDraftListingAsync(client, CategorySeedData.PhonesAndTabletsId, PromotionPlanSeedData.StandardId);
        var promoteAfterRevoke = await client.PostAsJsonAsync($"/api/v1/listings/{anotherListing.Id}/promote/tiktok", promoteRequest);
        promoteAfterRevoke.StatusCode.Should().Be(HttpStatusCode.Conflict, "a revoked connection must also block promoting a new listing");
    }

    private async Task RunMetricsSyncJobAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<AdCampaignMetricsSyncJob>();
        await job.RunAsync();
    }

    private async Task<AdPerformanceResponse> GetAdPerformanceAsync(HttpClient client, Guid listingId)
    {
        var response = await client.GetAsync($"/api/v1/listings/{listingId}/ad-performance");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AdPerformanceResponse>(JsonOptions))!;
    }

    private async Task<Guid> GetSellerProfileIdAsync(HttpClient sellerClient)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();

        var meResponse = await sellerClient.GetAsync("/api/v1/me");
        var me = await meResponse.Content.ReadFromJsonAsync<Ogasela.Application.Accounts.GetCurrentUser.CurrentUserResponse>(JsonOptions);

        var sellerProfile = await dbContext.SellerProfiles.FirstAsync(s => s.UserId == me!.Id);
        return sellerProfile.Id;
    }
}
