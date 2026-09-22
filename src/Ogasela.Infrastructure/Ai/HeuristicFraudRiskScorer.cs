using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Listings;
using Ogasela.Shared;

namespace Ogasela.Infrastructure.Ai;

/// <summary>
/// First-pass IFraudRiskScorer: a deterministic, explainable heuristic rather than an ML/LLM
/// classifier - explicitly not final, but gives ListingPublishService's Phase 11 handoff
/// something real to run on now. Four independent signals, each contributing a fixed weight
/// (capped at 1.0 total) so the score - and which reasons fired - stays easy to reason about:
///
///   - Price far below comparable listings (&lt; half the cheapest comparable): +0.40
///   - Suspicious off-platform/payment language in the title or description:      +0.30
///   - No photos attached:                                                         +0.15
///   - Seller account created within the last 7 days:                             +0.15
///
/// A real classifier (or a blended LLM pass) is a natural next iteration once there's labeled
/// outcome data to tune against.
/// </summary>
public sealed class HeuristicFraudRiskScorer : IFraudRiskScorer
{
    private const decimal LowPriceWeight = 0.40m;
    private const decimal SuspiciousLanguageWeight = 0.30m;
    private const decimal NoPhotosWeight = 0.15m;
    private const decimal NewAccountWeight = 0.15m;

    private static readonly TimeSpan NewAccountWindow = TimeSpan.FromDays(7);

    private static readonly string[] SuspiciousPhrases =
    [
        "wire transfer", "western union", "cash only", "no returns", "no refunds",
        "urgent sale", "outside the platform", "outside this platform", "whatsapp me directly",
        "pay before you see it", "money gram", "moneygram"
    ];

    private readonly IApplicationDbContext _dbContext;
    private readonly IPriceSuggestionService _priceSuggestionService;
    private readonly IDateTime _dateTime;

    public HeuristicFraudRiskScorer(
        IApplicationDbContext dbContext, IPriceSuggestionService priceSuggestionService, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _priceSuggestionService = priceSuggestionService;
        _dateTime = dateTime;
    }

    public async Task<FraudRiskAssessment> ScoreAsync(Listing listing, CancellationToken cancellationToken)
    {
        var score = 0m;
        var reasons = new List<string>();

        if (listing.Price is { } price)
        {
            var suggestion = await _priceSuggestionService.SuggestPriceRangeAsync(
                listing.CategoryId, listing.Title, listing.Condition, cancellationToken);

            if (suggestion.IsSuccess && price < suggestion.Value.MinKobo * 0.5m)
            {
                score += LowPriceWeight;
                reasons.Add("Price is far below comparable listings in this category");
            }
        }

        var haystack = $"{listing.Title} {listing.Description}";
        if (SuspiciousPhrases.Any(phrase => haystack.Contains(phrase, StringComparison.OrdinalIgnoreCase)))
        {
            score += SuspiciousLanguageWeight;
            reasons.Add("Title/description contains language associated with off-platform payment scams");
        }

        if (listing.MediaUrls.Count == 0)
        {
            score += NoPhotosWeight;
            reasons.Add("Listing has no photos");
        }

        var seller = await _dbContext.SellerProfiles.FirstOrDefaultAsync(s => s.Id == listing.SellerId, cancellationToken);
        if (seller is not null && _dateTime.UtcNow - seller.CreatedAt < NewAccountWindow)
        {
            score += NewAccountWeight;
            reasons.Add("Seller account was created very recently");
        }

        score = Math.Clamp(score, 0m, 1m);
        var reasonSummary = reasons.Count == 0 ? "No fraud risk signals detected" : string.Join("; ", reasons);

        return new FraudRiskAssessment(score, reasonSummary);
    }
}
