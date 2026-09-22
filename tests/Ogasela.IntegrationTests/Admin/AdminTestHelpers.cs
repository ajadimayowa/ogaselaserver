using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Ogasela.Api.Contracts.Accounts;
using Ogasela.Application.Accounts;
using Ogasela.Application.Accounts.AdminLogin;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Infrastructure.Persistence;
using static Ogasela.IntegrationTests.Listings.ListingsTestHelpers;

namespace Ogasela.IntegrationTests.Admin;

internal static class AdminTestHelpers
{
    private const string Password = "Sup3rSecret!";

    /// <summary>
    /// Internal roles (Moderator/FinanceAdmin/SuperAdmin) have no public self-registration path
    /// (RegisterUserCommandValidator only allows Buyer/Seller) - by design, these accounts are
    /// meant to be provisioned out-of-band, so tests seed the User row directly. Login itself
    /// goes through the real two-factor admin flow (email+password, then the OTP it sends) -
    /// the plain /auth/login endpoint always rejects internal roles with User.MfaRequired, so
    /// there's no shortcut around this even in tests.
    /// </summary>
    public static async Task<(HttpClient Client, Guid UserId)> RegisterInternalUserAsync(
        WebApplicationFactory<Program> factory, UserRole role)
    {
        var phone = GenerateNigerianPhoneNumber();
        var email = $"{Guid.NewGuid():N}@ogasela-admin-test.com";
        Guid userId;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            var user = User.Create(phone, email, passwordHasher.Hash(Password), role);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            userId = user.Id;
        }

        var client = factory.CreateClient();

        var adminLoginResponse = await client.PostAsJsonAsync("/api/v1/auth/admin/login", new AdminLoginRequest(email, Password));
        adminLoginResponse.EnsureSuccessStatusCode();

        string code;
        using (var scope = factory.Services.CreateScope())
        {
            var otpService = scope.ServiceProvider.GetRequiredService<IOtpService>();
            code = (await otpService.PeekAsync(phone, CancellationToken.None))!;
        }

        var verifyResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/admin/login/verify", new VerifyAdminLoginOtpRequest(email, code));
        verifyResponse.EnsureSuccessStatusCode();
        var tokens = await verifyResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        return (client, userId);
    }

    private static string GenerateNigerianPhoneNumber()
    {
        var suffix = Random.Shared.Next(0, 100_000_000).ToString("D8");
        return $"081{suffix}";
    }
}
