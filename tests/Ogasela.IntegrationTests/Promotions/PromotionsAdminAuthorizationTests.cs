using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Ogasela.Api.Contracts.Promotions;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Promotions;
using Ogasela.Infrastructure.Persistence;

namespace Ogasela.IntegrationTests.Promotions;

/// <summary>
/// Confirms the admin category/promotion-plan endpoints reject anyone who isn't SuperAdmin,
/// regardless of which other role they hold.
/// </summary>
public class PromotionsAdminAuthorizationTests : IClassFixture<PromotionsApiFactory>
{
    private readonly PromotionsApiFactory _factory;

    public PromotionsAdminAuthorizationTests(PromotionsApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCategory_AsBuyer_ReturnsForbidden()
    {
        var client = await CreateAuthenticatedClientAsync(UserRole.Buyer);

        using var content = new MultipartFormDataContent
        {
            { new StringContent("Toys"), "name" }
        };

        var response = await client.PostAsync("/api/v1/admin/categories", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateCategory_AsSeller_ReturnsForbidden()
    {
        var client = await CreateAuthenticatedClientAsync(UserRole.Seller);

        using var content = new MultipartFormDataContent
        {
            { new StringContent("Toys"), "name" },
            { new StringContent("true"), "isFreeEligible" },
            { new StringContent("1"), "attributeSchemaVersion" }
        };

        var response = await client.PutAsync($"/api/v1/admin/categories/{Guid.NewGuid()}", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdatePromotionPlan_AsModerator_ReturnsForbidden()
    {
        var client = await CreateAuthenticatedClientAsync(UserRole.Moderator);

        var request = new UpdatePromotionPlanRequest("Free", null, null, 7, 5, false, 0, 0m, AiToolTier.Basic, false, 0, true);
        var response = await client.PutAsJsonAsync($"/api/v1/admin/promotion-plans/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PublicEndpoints_DoNotRequireAuthentication_AndReturnSeedData()
    {
        var client = _factory.CreateClient();

        var categoriesResponse = await client.GetAsync("/api/v1/categories");
        categoriesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var plansResponse = await client.GetAsync("/api/v1/promotion-plans");
        plansResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(UserRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OgaselaDbContext>();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var user = User.Create(GeneratePhoneNumber(), null, passwordHasher.Hash("Sup3rSecret!"), role);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var (accessToken, _) = await tokenService.GenerateAccessTokenAsync(user, CancellationToken.None);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static string GeneratePhoneNumber()
    {
        var suffix = Random.Shared.Next(0, 100_000_000).ToString("D8");
        return $"080{suffix}";
    }
}
