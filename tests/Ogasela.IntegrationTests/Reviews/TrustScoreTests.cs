using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ogasela.Api.Contracts.Reviews;
using Ogasela.Application.Reviews.CreateReview;
using Ogasela.Infrastructure.Persistence;
using Ogasela.Infrastructure.Promotions.Seed;
using Ogasela.Infrastructure.Reviews;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Reviews;

public class TrustScoreTests : IClassFixture<ReviewsAndNotificationsApiFactory>
{
    private readonly ReviewsAndNotificationsApiFactory _factory;

    public TrustScoreTests(ReviewsAndNotificationsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RecomputeTrustScoresJob_ReflectsANewReviewsAverageRating()
    {
        var sellerClient = await RegisterAndVerifySellerAsync(_factory);
        var listing = await CreateAndPublishFreeListingAsync(sellerClient, CategorySeedData.PhonesAndTabletsId);

        var sellerProfileId = await GetSellerProfileIdAsync(sellerClient);

        // No reviews yet: Verified (20) + neutral rating midpoint (25) + neutral response midpoint (15) = 60.
        await RunTrustScoreJobAsync();
        var scoreWithNoReviews = await GetTrustScoreAsync(sellerProfileId);
        scoreWithNoReviews.Should().Be(60m);

        var firstBuyer = await RegisterSellerAsync(_factory);
        var firstReview = await firstBuyer.PostAsJsonAsync(
            "/api/v1/reviews", new CreateReviewRequest(listing.Id, 5, "Excellent seller!"));
        firstReview.StatusCode.Should().Be(HttpStatusCode.OK);

        // One 5-star review: Verified (20) + rating (5/5*50=50) + neutral response (15) = 85.
        await RunTrustScoreJobAsync();
        var scoreWithOneReview = await GetTrustScoreAsync(sellerProfileId);
        scoreWithOneReview.Should().Be(85m);

        var secondBuyer = await RegisterSellerAsync(_factory);
        var secondReview = await secondBuyer.PostAsJsonAsync(
            "/api/v1/reviews", new CreateReviewRequest(listing.Id, 1, "Not what I expected."));
        secondReview.StatusCode.Should().Be(HttpStatusCode.OK);

        // Average rating is now (5+1)/2=3: Verified (20) + rating (3/5*50=30) + neutral response (15) = 65.
        await RunTrustScoreJobAsync();
        var scoreWithTwoReviews = await GetTrustScoreAsync(sellerProfileId);
        scoreWithTwoReviews.Should().Be(65m);
        scoreWithTwoReviews.Should().NotBe(scoreWithOneReview, "adding a low review must pull the average - and the score - down");
    }

    private async Task RunTrustScoreJobAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<RecomputeTrustScoresJob>();
        await job.RunAsync();
    }

    private async Task<decimal> GetTrustScoreAsync(Guid sellerProfileId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();
        var trustScore = await dbContext.TrustScores.FirstAsync(t => t.SellerId == sellerProfileId);
        return trustScore.Score;
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
