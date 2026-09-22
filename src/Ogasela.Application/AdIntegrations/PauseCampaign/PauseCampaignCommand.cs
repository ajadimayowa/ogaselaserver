using MediatR;
using Ogasela.Application.AdIntegrations.PromoteListing;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.PauseCampaign;

public sealed record PauseCampaignCommand(Guid ListingId, AdPlatform Platform) : IRequest<Result<AdCampaignResponse>>;
