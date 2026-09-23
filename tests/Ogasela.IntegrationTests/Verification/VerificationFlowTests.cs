using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Ogasela.Api.Contracts.Accounts;
using Ogasela.Api.Contracts.Verification;
using Ogasela.Application.Accounts;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Accounts.RegisterUser;
using Ogasela.Application.Verification.GetVerificationStatus;
using Ogasela.Application.Verification.ManualReviewDecision;
using Ogasela.Application.Verification.SubmitConsent;
using Ogasela.Application.Verification.SubmitVerification;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Verification;
using Ogasela.Infrastructure.Persistence;

namespace Ogasela.IntegrationTests.Verification;

public class VerificationFlowTests : IClassFixture<VerificationApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly VerificationApiFactory _factory;

    public VerificationFlowTests(VerificationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConsentThenSubmit_WithHighSimilarityAndPassingLiveness_AutomaticallyVerifies()
    {
        var client = await CreateAuthenticatedSellerClientAsync();

        var consentResponse = await client.PostAsJsonAsync("/api/v1/verification/consent", new { });
        consentResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var submitResponse = await SubmitAsync(client, "selfie.jpg", "id.jpg", "liveness-ok-session");
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var submitted = await submitResponse.Content.ReadFromJsonAsync<SubmitVerificationResponse>(JsonOptions);
        submitted!.Decision.Should().Be(VerificationDecision.Verified);

        var status = await GetStatusAsync(client);
        status.Status.Should().Be(Ogasela.Domain.Accounts.VerificationStatus.Verified);
    }

    [Fact]
    public async Task ConsentThenSubmit_WithMidRangeSimilarity_GoesToManualReviewThenModeratorApproves()
    {
        var client = await CreateAuthenticatedSellerClientAsync();

        await client.PostAsJsonAsync("/api/v1/verification/consent", new { });

        var submitResponse = await SubmitAsync(client, "selfie-score85.jpg", "id.jpg", "liveness-ok-session");
        var submitted = await submitResponse.Content.ReadFromJsonAsync<SubmitVerificationResponse>(JsonOptions);
        submitted!.Decision.Should().Be(VerificationDecision.ManualReview);

        var statusAfterSubmit = await GetStatusAsync(client);
        statusAfterSubmit.Status.Should().Be(Ogasela.Domain.Accounts.VerificationStatus.ManualReview);

        var moderatorClient = await CreateAuthenticatedModeratorClientAsync();
        var reviewResponse = await moderatorClient.PostAsJsonAsync(
            $"/api/v1/verification/{submitted.VerificationId}/manual-review",
            new ManualReviewRequest(VerificationDecision.Verified, "Manually confirmed"),
            JsonOptions);
        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusAfterReview = await GetStatusAsync(client);
        statusAfterReview.Status.Should().Be(Ogasela.Domain.Accounts.VerificationStatus.Verified);
    }

    [Fact]
    public async Task ConsentThenSubmit_WithLowSimilarity_FailsTwiceThenIsRoutedToManualReviewOnTheThirdAttempt()
    {
        var client = await CreateAuthenticatedSellerClientAsync();

        await client.PostAsJsonAsync("/api/v1/verification/consent", new { });

        for (var attempt = 1; attempt <= 2; attempt++)
        {
            var response = await SubmitAsync(client, "selfie-score60.jpg", "id.jpg", "liveness-ok-session");
            var submitted = await response.Content.ReadFromJsonAsync<SubmitVerificationResponse>(JsonOptions);
            submitted!.Decision.Should().Be(VerificationDecision.Failed);
            submitted.AttemptNumber.Should().Be(attempt);
        }

        var thirdResponse = await SubmitAsync(client, "selfie-score60.jpg", "id.jpg", "liveness-ok-session");
        var thirdSubmitted = await thirdResponse.Content.ReadFromJsonAsync<SubmitVerificationResponse>(JsonOptions);
        thirdSubmitted!.AttemptNumber.Should().Be(3);
        thirdSubmitted.Decision.Should().Be(VerificationDecision.ManualReview);

        var status = await GetStatusAsync(client);
        status.Status.Should().Be(Ogasela.Domain.Accounts.VerificationStatus.ManualReview);
        status.AttemptsUsed.Should().Be(3);
    }

    private async Task<VerificationStatusResponse> GetStatusAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/verification/status");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await response.Content.ReadFromJsonAsync<VerificationStatusResponse>(JsonOptions);
        return status!;
    }

    private static async Task<HttpResponseMessage> SubmitAsync(
        HttpClient client, string selfieFileName, string idPhotoFileName, string livenessSessionRef)
    {
        using var content = new MultipartFormDataContent();

        var selfieContent = new ByteArrayContent(Encoding.UTF8.GetBytes("selfie-bytes"));
        selfieContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(selfieContent, "selfie", selfieFileName);

        var idPhotoContent = new ByteArrayContent(Encoding.UTF8.GetBytes("id-photo-bytes"));
        idPhotoContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(idPhotoContent, "idPhoto", idPhotoFileName);

        content.Add(new StringContent(livenessSessionRef), "livenessSessionRef");

        return await client.PostAsync("/api/v1/verification/submit", content);
    }

    private async Task<HttpClient> CreateAuthenticatedSellerClientAsync()
    {
        var client = _factory.CreateClient();
        var phone = GenerateNigerianPhoneNumber();
        const string password = "Sup3rSecret!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(phone, null, password, UserRole.Seller, "Ada's Fabrics", null, null));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        await client.PostAsJsonAsync("/api/v1/auth/otp/request", new RequestOtpRequest(phone));

        using (var scope = _factory.Services.CreateScope())
        {
            var otpService = scope.ServiceProvider.GetRequiredService<IOtpService>();
            var code = await otpService.PeekAsync(phone, CancellationToken.None);
            var verifyResponse = await client.PostAsJsonAsync(
                "/api/v1/auth/otp/verify", new VerifyOtpRequest(phone, code!));
            verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(phone, null, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var otpService = scope.ServiceProvider.GetRequiredService<IOtpService>();
            var loginCode = await otpService.PeekAsync(phone, CancellationToken.None);
            var verifyLoginResponse = await client.PostAsJsonAsync(
                "/api/v1/auth/login/verify", new VerifyLoginOtpRequest(phone, null, loginCode!));
            var tokens = await verifyLoginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        }

        return client;
    }

    /// <summary>
    /// Moderator accounts aren't self-serve (registration only allows Buyer/Seller), so this
    /// seeds one directly and mints its token the same way login would, rather than adding a
    /// backdoor admin-creation endpoint just for tests.
    /// </summary>
    private async Task<HttpClient> CreateAuthenticatedModeratorClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var moderator = User.Create(
            GenerateNigerianPhoneNumber(), null, passwordHasher.Hash("Sup3rSecret!"), UserRole.Moderator);
        dbContext.Users.Add(moderator);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var (accessToken, _) = await tokenService.GenerateAccessTokenAsync(moderator, CancellationToken.None);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static string GenerateNigerianPhoneNumber()
    {
        var suffix = Random.Shared.Next(0, 100_000_000).ToString("D8");
        return $"080{suffix}";
    }
}
