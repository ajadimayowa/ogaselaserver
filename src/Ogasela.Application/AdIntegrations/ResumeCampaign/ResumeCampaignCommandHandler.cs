using MediatR;
using Ogasela.Application.AdIntegrations.Internal;
using Ogasela.Application.AdIntegrations.PromoteListing;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.ResumeCampaign;

public sealed class ResumeCampaignCommandHandler : IRequestHandler<ResumeCampaignCommand, Result<AdCampaignResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAdPlatformClientFactory _clientFactory;
    private readonly ITokenEncryptor _tokenEncryptor;

    public ResumeCampaignCommandHandler(
        IApplicationDbContext dbContext, ICurrentUserService currentUser,
        IAdPlatformClientFactory clientFactory, ITokenEncryptor tokenEncryptor)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clientFactory = clientFactory;
        _tokenEncryptor = tokenEncryptor;
    }

    public async Task<Result<AdCampaignResponse>> Handle(ResumeCampaignCommand request, CancellationToken cancellationToken)
    {
        var listingResult = await AdIntegrationLookups.GetOwnedListingAsync(_dbContext, _currentUser, request.ListingId, cancellationToken);
        if (listingResult.IsFailure)
        {
            return Result.Failure<AdCampaignResponse>(listingResult.Error);
        }

        var listing = listingResult.Value;

        var campaign = await AdIntegrationLookups.GetLatestCampaignAsync(_dbContext, listing.Id, request.Platform, cancellationToken);
        if (campaign is null || campaign.Status == AdCampaignStatus.Ended)
        {
            return Result.Failure<AdCampaignResponse>(AdIntegrationErrors.CampaignNotFound);
        }

        var connectionResult = await AdIntegrationLookups.RequireActiveConnectionAsync(_dbContext, listing.SellerId, request.Platform, cancellationToken);
        if (connectionResult.IsFailure)
        {
            return Result.Failure<AdCampaignResponse>(connectionResult.Error);
        }

        var connection = connectionResult.Value;

        var client = _clientFactory.GetClient(request.Platform);
        var accessToken = _tokenEncryptor.Decrypt(connection.EncryptedAccessToken);

        var resumeResult = await client.ResumeCampaignAsync(accessToken, campaign.ExternalCampaignId, cancellationToken);
        if (resumeResult.IsFailure)
        {
            return Result.Failure<AdCampaignResponse>(resumeResult.Error);
        }

        campaign.Resume();
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(AdCampaignResponse.From(campaign));
    }
}
