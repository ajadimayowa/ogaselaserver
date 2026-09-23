using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ogasela.Api.Contracts.Admin;
using Ogasela.Api.Contracts.Messaging;
using Ogasela.Application.AdIntegrations;
using Ogasela.Application.Admin.EraseUserData;
using Ogasela.Application.Moderation.GetModerationQueue;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Moderation;
using Ogasela.Domain.Verification;
using Ogasela.Infrastructure.Persistence;
using static Ogasela.IntegrationTests.Admin.AdminTestHelpers;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Admin;

public class AdminModerationTests : IClassFixture<AdminApiFactory>
{
    private readonly AdminApiFactory _factory;

    public AdminModerationTests(AdminApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ModerationQueue_ContainsBothAReportAndAPendingManualReviewItem()
    {
        var (reporterClient, _) = await RegisterAndLoginBuyerAsync();
        var (_, reportedUserId) = await RegisterAndLoginBuyerAsync();

        var reportResponse = await reporterClient.PostAsJsonAsync(
            "/api/v1/reports", new ReportUserRequest(reportedUserId, "Suspicious behaviour"));
        reportResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sellerId = await SeedPendingManualReviewAsync();

        var (moderatorClient, _) = await RegisterInternalUserAsync(_factory, UserRole.Moderator);
        var queue = await GetModerationQueueAsync(moderatorClient);

        queue.Should().Contain(i => i.Type == ModerationQueueItemType.Report && i.TargetId == reportedUserId);
        queue.Should().Contain(i => i.Type == ModerationQueueItemType.BiometricManualReview && i.TargetId == sellerId);
    }

    [Fact]
    public async Task ResolveReport_WritesAnAuditLogEntry()
    {
        var (reporterClient, _) = await RegisterAndLoginBuyerAsync();
        var (_, reportedUserId) = await RegisterAndLoginBuyerAsync();

        var reportResponse = await reporterClient.PostAsJsonAsync(
            "/api/v1/reports", new ReportUserRequest(reportedUserId, "Spam messages"));
        var reportId = await reportResponse.Content.ReadFromJsonAsync<Guid>();

        var (moderatorClient, moderatorId) = await RegisterInternalUserAsync(_factory, UserRole.Moderator);

        var decisionResponse = await moderatorClient.PostAsJsonAsync(
            $"/api/v1/admin/moderation/{reportId}/decision", new ResolveReportRequest(ReportDecision.Approve, "Reviewed, confirmed valid"));
        decisionResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();

        var auditEntry = await dbContext.AuditLogs.FirstOrDefaultAsync(
            a => a.TargetId == reportId && a.Action == "Report.Approve");
        auditEntry.Should().NotBeNull();
        auditEntry!.ActorId.Should().Be(moderatorId);

        var report = await dbContext.Reports.FirstAsync(r => r.Id == reportId);
        report.Status.Should().Be(ReportStatus.Resolved);
    }

    [Fact]
    public async Task NonPrivilegedRole_IsRejectedWith403OnEveryAdminRoute()
    {
        var (buyerClient, _) = await RegisterAndLoginBuyerAsync();
        var randomId = Guid.NewGuid();

        var routes = new (HttpMethod Method, string Path)[]
        {
            (HttpMethod.Get, "/api/v1/admin/moderation/queue"),
            (HttpMethod.Post, $"/api/v1/admin/moderation/{randomId}/decision"),
            (HttpMethod.Get, "/api/v1/admin/verification/manual-review-queue"),
            (HttpMethod.Get, "/api/v1/admin/dashboard"),
            (HttpMethod.Post, $"/api/v1/admin/data-requests/{randomId}/erase"),
            (HttpMethod.Get, "/api/v1/admin/audit-log")
        };

        foreach (var (method, path) in routes)
        {
            using var request = new HttpRequestMessage(method, path);
            if (method == HttpMethod.Post)
            {
                request.Content = JsonContent.Create(new ResolveReportRequest(ReportDecision.Approve, null));
            }

            var response = await buyerClient.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"a Buyer must not be allowed to call {method} {path}");
        }
    }

    [Fact]
    public async Task EraseUserDataCommand_RemovesExpectedRecordsAndProducesAuditTrail()
    {
        var sellerClient = await RegisterAndVerifySellerAsync(_factory);
        var meResponse = await sellerClient.GetAsync("/api/v1/me");
        var me = await meResponse.Content.ReadFromJsonAsync<Ogasela.Application.Accounts.GetCurrentUser.CurrentUserResponse>(JsonOptions);
        var userId = me!.Id;

        Guid sellerProfileId;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();
            var tokenEncryptor = scope.ServiceProvider.GetRequiredService<ITokenEncryptor>();

            var sellerProfile = await dbContext.SellerProfiles.FirstAsync(s => s.UserId == userId);
            sellerProfileId = sellerProfile.Id;

            var connection = AdAccountConnection.Create(
                Guid.NewGuid(), sellerProfileId, AdPlatform.Facebook,
                tokenEncryptor.Encrypt("fake-access-token"), null, "fake-external-account", DateTime.UtcNow);
            dbContext.AdAccountConnections.Add(connection);
            await dbContext.SaveChangesAsync();
        }

        var (superAdminClient, superAdminId) = await RegisterInternalUserAsync(_factory, UserRole.SuperAdmin);

        var eraseResponse = await superAdminClient.PostAsync($"/api/v1/admin/data-requests/{userId}/erase", content: null);
        eraseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var receipt = await eraseResponse.Content.ReadFromJsonAsync<EraseUserDataReceipt>(JsonOptions);
        receipt!.BiometricVerificationsErased.Should().BeGreaterThanOrEqualTo(1);
        receipt.AdAccountConnectionsRevoked.Should().Be(1);
        receipt.SellerProfileAnonymized.Should().BeTrue();

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<OgaselaDbContext>();

        var user = await verifyDbContext.Users.FirstAsync(u => u.Id == userId);
        user.Phone.Should().BeNull();
        user.Email.Should().BeNull();

        var sellerProfileAfter = await verifyDbContext.SellerProfiles.FirstAsync(s => s.Id == sellerProfileId);
        sellerProfileAfter.BusinessName.Should().Be("Erased Seller");
        sellerProfileAfter.RcNumber.Should().BeNull();
        sellerProfileAfter.Nin.Should().BeNull();

        var verifications = await verifyDbContext.BiometricVerifications.Where(v => v.SellerId == sellerProfileId).ToListAsync();
        verifications.Should().NotBeEmpty();
        verifications.Should().OnlyContain(v => v.IsErased && v.SelfieImageS3Key == null && v.IdPhotoImageS3Key == null);

        var connectionAfter = await verifyDbContext.AdAccountConnections.FirstAsync(c => c.SellerId == sellerProfileId);
        connectionAfter.RevokedAt.Should().NotBeNull();

        var auditEntry = await verifyDbContext.AuditLogs.FirstOrDefaultAsync(
            a => a.TargetId == userId && a.Action == "User.DataErasure");
        auditEntry.Should().NotBeNull();
        auditEntry!.ActorId.Should().Be(superAdminId);
    }

    private async Task<Guid> SeedPendingManualReviewAsync()
    {
        var sellerClient = await RegisterSellerAsync(_factory);
        var meResponse = await sellerClient.GetAsync("/api/v1/me");
        var me = await meResponse.Content.ReadFromJsonAsync<Ogasela.Application.Accounts.GetCurrentUser.CurrentUserResponse>(JsonOptions);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();

        var sellerProfile = await dbContext.SellerProfiles.FirstAsync(s => s.UserId == me!.Id);

        var verification = BiometricVerification.Create(
            sellerProfile.Id, attemptNumber: 1, "selfie-key", "id-photo-key",
            livenessScore: 60m, idMatchScore: 55m, VerificationDecision.ManualReview, DateTime.UtcNow, rawImageRetentionDays: 30);

        dbContext.BiometricVerifications.Add(verification);
        await dbContext.SaveChangesAsync();

        return sellerProfile.Id;
    }

    private async Task<(HttpClient Client, Guid UserId)> RegisterAndLoginBuyerAsync()
    {
        var client = _factory.CreateClient();
        var phone = $"080{Random.Shared.Next(0, 100_000_000):D8}";
        const string password = "Sup3rSecret!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new Ogasela.Api.Contracts.Accounts.RegisterRequest(phone, null, password, UserRole.Buyer, null, null, null));
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new Ogasela.Api.Contracts.Accounts.LoginRequest(phone, null, password));
        loginResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var otpService = scope.ServiceProvider.GetRequiredService<Ogasela.Application.Accounts.Interfaces.IOtpService>();
        var code = await otpService.PeekAsync(phone, CancellationToken.None);

        var verifyLoginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login/verify", new Ogasela.Api.Contracts.Accounts.VerifyLoginOtpRequest(phone, null, code!));
        verifyLoginResponse.EnsureSuccessStatusCode();
        var tokens = await verifyLoginResponse.Content.ReadFromJsonAsync<Ogasela.Application.Accounts.AuthTokenResponse>();

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var meResponse = await client.GetAsync("/api/v1/me");
        var me = await meResponse.Content.ReadFromJsonAsync<Ogasela.Application.Accounts.GetCurrentUser.CurrentUserResponse>(JsonOptions);

        return (client, me!.Id);
    }

    private static async Task<IReadOnlyList<ModerationQueueItemResponse>> GetModerationQueueAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/admin/moderation/queue");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<ModerationQueueItemResponse>>(JsonOptions))!;
    }
}
