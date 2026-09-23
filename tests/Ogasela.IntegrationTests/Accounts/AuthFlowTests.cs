using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Ogasela.Api.Contracts.Accounts;
using Ogasela.Application.Accounts;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Accounts.RegisterUser;
using Ogasela.Domain.Accounts;

namespace Ogasela.IntegrationTests.Accounts;

public class AuthFlowTests : IClassFixture<AccountsApiFactory>
{
    // Must start from the Web defaults (camelCase, case-insensitive property matching) -
    // ReadFromJsonAsync's parameterless overload uses those implicitly, but a fresh
    // `new JsonSerializerOptions()` does not, so a bare options object here would silently
    // fail to bind "role" onto the Role property. The Api serializes enums as strings (see
    // Program.cs's JsonStringEnumConverter), so this needs the same converter to match.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly AccountsApiFactory _factory;

    public AuthFlowTests(AccountsApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(UserRole.Buyer)]
    [InlineData(UserRole.Seller)]
    public async Task FullAuthFlow_RegisterOtpLoginRefreshLogout_Succeeds(UserRole accountType)
    {
        var client = _factory.CreateClient();
        var phone = GenerateNigerianPhoneNumber();
        const string password = "Sup3rSecret!";

        // 1. Register
        var registerRequest = new RegisterRequest(
            Phone: phone,
            Email: null,
            Password: password,
            AccountType: accountType,
            BusinessName: accountType == UserRole.Seller ? "Ada's Fabrics" : null,
            RcNumber: null,
            Nin: null);

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registered = await registerResponse.Content.ReadFromJsonAsync<RegisterUserResponse>(JsonOptions);
        registered.Should().NotBeNull();
        registered!.Role.Should().Be(accountType);

        // 2. Request OTP
        var otpRequestResponse = await client.PostAsJsonAsync("/api/v1/auth/otp/request", new RequestOtpRequest(phone));
        otpRequestResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // No SMS gateway exists yet (Phase 1 scope); read the code straight out of the OTP
        // store the same way a later phase's SMS gateway integration test would intercept it.
        using var scope = _factory.Services.CreateScope();
        var otpService = scope.ServiceProvider.GetRequiredService<IOtpService>();
        var code = await otpService.PeekAsync(phone, CancellationToken.None);
        code.Should().NotBeNullOrEmpty();

        // 3. Verify OTP
        var verifyResponse = await client.PostAsJsonAsync("/api/v1/auth/otp/verify", new VerifyOtpRequest(phone, code!));
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Login (step 1: password -> OTP sent)
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(phone, null, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginCode = await otpService.PeekAsync(phone, CancellationToken.None);
        loginCode.Should().NotBeNullOrEmpty();

        // 4b. Login (step 2: OTP -> tokens)
        var verifyLoginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login/verify", new VerifyLoginOtpRequest(phone, null, loginCode!));
        verifyLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await verifyLoginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // GET /me works with the freshly issued access token.
        client.DefaultRequestHeaders.Authorization = new("Bearer", tokens.AccessToken);
        var meResponse = await client.GetAsync("/api/v1/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Refresh - rotates the refresh token.
        var refreshResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(tokens.RefreshToken));
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedTokens = await refreshResponse.Content.ReadFromJsonAsync<AuthTokenResponse>();
        rotatedTokens.Should().NotBeNull();
        rotatedTokens!.RefreshToken.Should().NotBe(tokens.RefreshToken);

        // 6. Logout with the current (rotated) refresh token.
        var logoutResponse = await client.PostAsJsonAsync("/api/v1/auth/logout", new LogoutRequest(rotatedTokens.RefreshToken));
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // The logged-out refresh token can no longer be used to refresh.
        var postLogoutRefresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(rotatedTokens.RefreshToken));
        postLogoutRefresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Replaying the original (pre-rotation) refresh token is reuse of an already-rotated
        // token and must be rejected too - unit tests cover that this also revokes the whole
        // token family; here we only confirm the endpoint rejects it.
        var reuseResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(tokens.RefreshToken));
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string GenerateNigerianPhoneNumber()
    {
        var suffix = Random.Shared.Next(0, 100_000_000).ToString("D8");
        return $"080{suffix}";
    }
}
