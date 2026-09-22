using Ogasela.Domain.AdIntegrations;

namespace Ogasela.Application.AdIntegrations;

public interface IAdPlatformClientFactory
{
    IAdPlatformClient GetClient(AdPlatform platform);
}
