using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ogasela.Application.Ai;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Payments;
using Ogasela.Application.Verification.Interfaces;
using Ogasela.Domain.Listings;
using Ogasela.Domain.Moderation;
using Ogasela.Domain.Promotions;
using Ogasela.Domain.Verification;
using Ogasela.Shared;

namespace Ogasela.Application.Listings;

/// <summary>
/// The single place PRS Section 5's publish rules are enforced, in order. Both
/// <c>PublishListingCommandHandler</c> (first publish, from Draft) and
/// <c>RepostListingCommandHandler</c> (re-publish, from Expired/Sold/Paused) call this after
/// their own status precondition passes, so a repost can never skip a rule a fresh publish
/// would have to pass.
/// </summary>
public sealed class ListingPublishService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IVerificationGuard _verificationGuard;
    private readonly IPaymentAuthorizer _paymentAuthorizer;
    private readonly IFraudRiskScorer _fraudRiskScorer;
    private readonly IDateTime _dateTime;
    private readonly ListingSettings _settings;
    private readonly AiSettings _aiSettings;

    public ListingPublishService(
        IApplicationDbContext dbContext,
        IVerificationGuard verificationGuard,
        IPaymentAuthorizer paymentAuthorizer,
        IFraudRiskScorer fraudRiskScorer,
        IDateTime dateTime,
        IOptions<ListingSettings> settings,
        IOptions<AiSettings> aiSettings)
    {
        _dbContext = dbContext;
        _verificationGuard = verificationGuard;
        _paymentAuthorizer = paymentAuthorizer;
        _fraudRiskScorer = fraudRiskScorer;
        _dateTime = dateTime;
        _settings = settings.Value;
        _aiSettings = aiSettings.Value;
    }

    public async Task<Result> PublishAsync(Listing listing, CancellationToken cancellationToken)
    {
        // RULE-01: the seller must be fully verified.
        try
        {
            await _verificationGuard.RequireVerifiedSellerAsync(listing.SellerId, cancellationToken);
        }
        catch (SellerNotVerifiedException)
        {
            return Result.Failure(ListingErrors.NotVerified);
        }

        // RULE-02: a plan must be selected.
        if (listing.PromotionPlanId is not { } promotionPlanId)
        {
            return Result.Failure(ListingErrors.NoPlanSelected);
        }

        var plan = await _dbContext.PromotionPlans.FirstOrDefaultAsync(p => p.Id == promotionPlanId, cancellationToken);
        if (plan is null)
        {
            return Result.Failure(ListingErrors.PromotionPlanNotFound);
        }

        if (plan.Name == PromotionPlanName.Free)
        {
            // RULE-04: the category must allow the Free plan.
            var category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == listing.CategoryId, cancellationToken);
            if (category is null)
            {
                return Result.Failure(ListingErrors.CategoryNotFound);
            }

            if (!category.IsFreeEligible)
            {
                return Result.Failure(ListingErrors.CategoryNotFreeEligible);
            }

            // RULE-05: cap on concurrently Active Free-plan listings.
            var activeFreeListings = await _dbContext.Listings.CountAsync(
                l => l.SellerId == listing.SellerId
                     && l.Id != listing.Id
                     && l.PromotionPlanId == plan.Id
                     && (l.Status == ListingStatus.Active || l.Status == ListingStatus.ExpiringSoon),
                cancellationToken);

            if (activeFreeListings >= _settings.FreePlanCap)
            {
                return Result.Failure(ListingErrors.FreePlanCapReached);
            }
        }
        else
        {
            // RULE-06: paid plans require authorized payment; never publish on failure.
            var paymentResult = await _paymentAuthorizer.AuthorizeAsync(listing.SellerId, plan.Id, listing.Id, cancellationToken);
            if (paymentResult.IsFailure)
            {
                return Result.Failure(ListingErrors.PaymentFailed);
            }
        }

        var now = _dateTime.UtcNow;
        listing.Publish(now, now.AddDays(plan.DurationDays));

        // Fraud-risk scoring is a platform safety net, not a seller-facing tool - it runs for
        // every publish regardless of plan tier and never blocks the listing. Above the
        // threshold, it only opens a Report for Phase 11's moderation queue to triage.
        var assessment = await _fraudRiskScorer.ScoreAsync(listing, cancellationToken);
        if (assessment.RiskScore >= _aiSettings.FraudRiskThreshold)
        {
            _dbContext.Reports.Add(Report.Create(
                reporterId: null, ReportTargetType.Listing, listing.Id, assessment.ReasonSummary, now, assessment.RiskScore));
        }

        return Result.Success();
    }
}
