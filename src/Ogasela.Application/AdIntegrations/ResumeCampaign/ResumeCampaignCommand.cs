using MediatR;
using Ogasela.Application.AdIntegrations.PromoteListing;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.ResumeCampaign;

public sealed record ResumeCampaignCommand(Guid ListingId, AdPlatform Platform) : IRequest<Result<AdCampaignResponse>>;
