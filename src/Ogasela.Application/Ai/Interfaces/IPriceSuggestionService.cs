using Ogasela.Domain.Listings;
using Ogasela.Shared;

namespace Ogasela.Application.Ai.Interfaces;

public interface IPriceSuggestionService
{
    Task<Result<PriceSuggestion>> SuggestPriceRangeAsync(
        Guid categoryId, string title, ListingCondition condition, CancellationToken cancellationToken);
}

/// <summary>All amounts in kobo, matching Listing.Price/PromotionPlan.Price.</summary>
public sealed record PriceSuggestion(decimal MinKobo, decimal SuggestedKobo, decimal MaxKobo, int ComparableCount);
