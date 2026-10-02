using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Ogasela.IntegrationTests.Accounts;

/// <summary>
/// The test host has no SocialAuth credentials, which is exactly the "buttons shipped before the
/// provider is set up" state - each provider must answer 503 ProviderNotConfigured, not 500.
/// The sign-in/link/create rules themselves are covered by SocialLoginCommandHandlerTests.
/// </summary>
public class SocialLoginTests : IClassFixture<AccountsApiFactory>
{
    private readonly AccountsApiFactory _factory;

    public SocialLoginTests(AccountsApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Google")]
    [InlineData("Facebook")]
    public async Task SocialLogin_WhenProviderIsNotConfigured_ReturnsServiceUnavailable(string provider)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/social", new { provider, token = "any-token" });

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task SocialLogin_WithInternalAccountType_IsRejectedByValidation()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/social", new { provider = "Google", token = "any-token", accountType = "SuperAdmin" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
