using Ogasela.Application.AdIntegrations;
using Ogasela.Domain.AdIntegrations;

namespace Ogasela.Infrastructure.AdIntegrations;

/// <summary>
/// Both FacebookAdsClient and TikTokAdsClient are registered as IAdPlatformClient (each
/// AddHttpClient&lt;IAdPlatformClient, T&gt; call is additive), so this just picks the one whose
/// Platform matches - same shape as IPaymentGatewayResolver for IPaymentGateway.
/// </summary>
public sealed class AdPlatformClientFactory : IAdPlatformClientFactory
{
    private readonly IReadOnlyList<IAdPlatformClient> _clients;

    public AdPlatformClientFactory(IEnumerable<IAdPlatformClient> clients)
    {
        _clients = clients.ToList();
    }

    public IAdPlatformClient GetClient(AdPlatform platform) =>
        _clients.FirstOrDefault(c => c.Platform == platform)
            ?? throw new InvalidOperationException($"No IAdPlatformClient is registered for platform '{platform}'.");
}
