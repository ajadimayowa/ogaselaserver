using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Ai;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Application.AdIntegrations.Internal;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Domain.Promotions;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.PromoteListing;

public sealed class PromoteListingCommandHandler : IRequestHandler<PromoteListingCommand, Result<AdCampaignResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;
    private readonly IAdPlatformClientFactory _clientFactory;
    private readonly ITokenEncryptor _tokenEncryptor;
    private readonly AiAccessGuard _aiAccessGuard;
    private readonly IListingCopyGenerator _copyGenerator;

    public PromoteListingCommandHandler(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, IDateTime dateTime,
        IAdPlatformClientFactory clientFactory, ITokenEncryptor tokenEncryptor,
        AiAccessGuard aiAccessGuard, IListingCopyGenerator copyGenerator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _clientFactory = clientFactory;
        _tokenEncryptor = tokenEncryptor;
        _aiAccessGuard = aiAccessGuard;
        _copyGenerator = copyGenerator;
    }

    public async Task<Result<AdCampaignResponse>> Handle(PromoteListingCommand request, CancellationToken cancellationToken)
    {
        var listingResult = await AdIntegrationLookups.GetOwnedListingAsync(_dbContext, _currentUser, request.ListingId, cancellationToken);
        if (listingResult.IsFailure)
        {
            return Result.Failure<AdCampaignResponse>(listingResult.Error);
        }

        var listing = listingResult.Value;

        var plan = listing.PromotionPlanId is null
            ? null
            : await _dbContext.PromotionPlans.FirstOrDefaultAsync(p => p.Id == listing.PromotionPlanId, cancellationToken);

        if (plan is null)
        {
            return Result.Failure<AdCampaignResponse>(Ogasela.Application.Listings.ListingErrors.NoPlanSelected);
        }

        if (!plan.AdPlatformPushAllowed)
        {
            return Result.Failure<AdCampaignResponse>(AdIntegrationErrors.PromotionNotAllowed);
        }

        var connectionResult = await AdIntegrationLookups.RequireActiveConnectionAsync(_dbContext, listing.SellerId, request.Platform, cancellationToken);
        if (connectionResult.IsFailure)
        {
            return Result.Failure<AdCampaignResponse>(connectionResult.Error);
        }

        var connection = connectionResult.Value;

        var (title, description) = await ResolveAdCopyAsync(request, listing.Title, listing.Description, plan, cancellationToken);

        var client = _clientFactory.GetClient(request.Platform);
        var accessToken = _tokenEncryptor.Decrypt(connection.EncryptedAccessToken);

        var createResult = await client.CreateCampaignAsync(
            new AdCampaignRequest(
                listing.Id, accessToken, connection.ExternalAccountId, title, description,
                listing.MediaUrls, request.BudgetKobo, request.DurationDays, request.Audience),
            cancellationToken);

        if (createResult.IsFailure)
        {
            return Result.Failure<AdCampaignResponse>(createResult.Error);
        }

        var campaign = AdCampaign.Create(
            Guid.NewGuid(), listing.Id, request.Platform, createResult.Value,
            request.BudgetKobo, request.DurationDays, _dateTime.UtcNow);

        _dbContext.AdCampaigns.Add(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(AdCampaignResponse.From(campaign));
    }

    /// <summary>Seller-supplied copy wins outright; otherwise this tries AI generation and falls back to the listing's own title/description on any failure - never a hard rejection.</summary>
    private async Task<(string Title, string Description)> ResolveAdCopyAsync(
        PromoteListingCommand request, string listingTitle, string listingDescription, PromotionPlan plan, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.AdCopyTitle) && !string.IsNullOrWhiteSpace(request.AdCopyDescription))
        {
            return (request.AdCopyTitle, request.AdCopyDescription);
        }

        var accessResult = await _aiAccessGuard.RequireCapabilityAsync(plan.Id, AiCapability.AdCopyAssist, cancellationToken);
        if (accessResult.IsFailure)
        {
            return (listingTitle, listingDescription);
        }

        var suggestionResult = await _copyGenerator.GenerateDescriptionAsync(listingTitle, null, cancellationToken);
        return suggestionResult.IsSuccess
            ? (suggestionResult.Value.Title, suggestionResult.Value.Description)
            : (listingTitle, listingDescription);
    }
}
