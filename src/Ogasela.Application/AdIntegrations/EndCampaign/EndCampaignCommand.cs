using MediatR;
using Ogasela.Application.AdIntegrations.PromoteListing;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.EndCampaign;

public sealed record EndCampaignCommand(Guid ListingId, AdPlatform Platform) : IRequest<Result<AdCampaignResponse>>;
