using Ogasela.Application.AdIntegrations;
using Ogasela.Domain.AdIntegrations;

namespace Ogasela.UnitTests.AdIntegrations;

public sealed class FakeAdPlatformClientFactory : IAdPlatformClientFactory
{
    private readonly IReadOnlyList<IAdPlatformClient> _clients;

    public FakeAdPlatformClientFactory(params IAdPlatformClient[] clients)
    {
        _clients = clients;
    }

    public IAdPlatformClient GetClient(AdPlatform platform) =>
        _clients.First(c => c.Platform == platform);
}
