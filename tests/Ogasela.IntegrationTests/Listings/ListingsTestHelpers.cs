using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Ogasela.Api.Contracts.Accounts;
using Ogasela.Api.Contracts.Listings;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Listings.GetListing;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Listings;

namespace Ogasela.IntegrationTests.Listings;

internal static class ListingsTestHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Registers a new Seller, logs in, and returns an authenticated client. Verification is not run.</summary>
    public static async Task<HttpClient> RegisterSellerAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var phone = GenerateNigerianPhoneNumber();
        const string password = "Sup3rSecret!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(phone, null, password, UserRole.Seller, "Ada's Fabrics", null, null));
        registerResponse.EnsureSuccessStatusCode();

        await client.PostAsJsonAsync("/api/v1/auth/otp/request", new RequestOtpRequest(phone));

        using (var scope = factory.Services.CreateScope())
        {
            var otpService = scope.ServiceProvider.GetRequiredService<IOtpService>();
            var code = await otpService.PeekAsync(phone, CancellationToken.None);
            var verifyOtpResponse = await client.PostAsJsonAsync(
                "/api/v1/auth/otp/verify", new VerifyOtpRequest(phone, code!));
            verifyOtpResponse.EnsureSuccessStatusCode();
        }

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(phone, null, password));
        loginResponse.EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var otpService = scope.ServiceProvider.GetRequiredService<IOtpService>();
            var code = await otpService.PeekAsync(phone, CancellationToken.None);
            var verifyLoginResponse = await client.PostAsJsonAsync(
                "/api/v1/auth/login/verify", new VerifyLoginOtpRequest(phone, null, code!));
            verifyLoginResponse.EnsureSuccessStatusCode();

            var tokens = await verifyLoginResponse.Content.ReadFromJsonAsync<Ogasela.Application.Accounts.AuthTokenResponse>();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        }

        return client;
    }

    /// <summary>Drives consent + a high-score biometric submission (via MockFaceVerificationProvider) to reach Verified.</summary>
    public static async Task VerifySellerAsync(HttpClient client)
    {
        var consentResponse = await client.PostAsJsonAsync("/api/v1/verification/consent", new { });
        consentResponse.EnsureSuccessStatusCode();

        using var content = new MultipartFormDataContent();

        var selfieContent = new ByteArrayContent(Encoding.UTF8.GetBytes("selfie-bytes"));
        selfieContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(selfieContent, "selfie", "selfie.jpg");

        var idPhotoContent = new ByteArrayContent(Encoding.UTF8.GetBytes("id-photo-bytes"));
        idPhotoContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(idPhotoContent, "idPhoto", "id.jpg");

        content.Add(new StringContent("liveness-ok-session"), "livenessSessionRef");

        var submitResponse = await client.PostAsync("/api/v1/verification/submit", content);
        submitResponse.EnsureSuccessStatusCode();
    }

    public static async Task<HttpClient> RegisterAndVerifySellerAsync(WebApplicationFactory<Program> factory)
    {
        var client = await RegisterSellerAsync(factory);
        await VerifySellerAsync(client);
        return client;
    }

    public static async Task<ListingResponse> CreateDraftListingAsync(
        HttpClient client, Guid categoryId, Guid? promotionPlanId, IReadOnlyList<string>? mediaUrls = null)
    {
        var request = new CreateListingRequest(
            "A great item", "In excellent condition", categoryId, 5000m, ListingCondition.Used,
            mediaUrls ?? ["https://example.com/photo1.jpg"], promotionPlanId);

        var response = await client.PostAsJsonAsync("/api/v1/listings", request);
        response.EnsureSuccessStatusCode();

        var listing = await response.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions);
        return listing!;
    }

    public static Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid listingId) =>
        client.PostAsync($"/api/v1/listings/{listingId}/publish", content: null);

    public static Task<HttpResponseMessage> RepostAsync(HttpClient client, Guid listingId) =>
        client.PostAsync($"/api/v1/listings/{listingId}/repost", content: null);

    public static Task<HttpResponseMessage> MarkSoldAsync(HttpClient client, Guid listingId) =>
        client.PostAsync($"/api/v1/listings/{listingId}/mark-sold", content: null);

    public static async Task<ListingResponse> CreateAndPublishFreeListingAsync(HttpClient client, Guid categoryId)
    {
        var listing = await CreateDraftListingAsync(client, categoryId, Ogasela.Infrastructure.Promotions.Seed.PromotionPlanSeedData.FreeId);
        var publishResponse = await PublishAsync(client, listing.Id);
        publishResponse.EnsureSuccessStatusCode();
        return (await publishResponse.Content.ReadFromJsonAsync<ListingResponse>(JsonOptions))!;
    }

    private static string GenerateNigerianPhoneNumber()
    {
        var suffix = Random.Shared.Next(0, 100_000_000).ToString("D8");
        return $"080{suffix}";
    }
}
