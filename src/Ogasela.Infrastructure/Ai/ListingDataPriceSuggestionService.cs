using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Ai;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Listings;
using Ogasela.Shared;

namespace Ogasela.Infrastructure.Ai;

/// <summary>
/// First-pass IPriceSuggestionService: queries actual Listing data for comparables rather than
/// calling out to any external pricing service. "Comparable" means same Category and Condition,
/// currently or previously live (Active/ExpiringSoon/Sold - a live or completed listing reflects
/// a real asking/closing price; Draft/Paused/Expired ones don't). Title isn't used to further
/// narrow the comparable set yet - a natural next step would be a trigram/full-text similarity
/// filter using the same search infrastructure Phase 6 built, once there's enough listing volume
/// for that to matter.
/// </summary>
public sealed class ListingDataPriceSuggestionService : IPriceSuggestionService
{
    private const int MinimumComparablesForCondition = 3;

    private readonly IApplicationDbContext _dbContext;

    public ListingDataPriceSuggestionService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PriceSuggestion>> SuggestPriceRangeAsync(
        Guid categoryId, string title, ListingCondition condition, CancellationToken cancellationToken)
    {
        var comparablePrices = await QueryComparablePricesAsync(categoryId, condition, cancellationToken);

        // Not enough same-condition data - broaden to the whole category rather than fail outright.
        if (comparablePrices.Count < MinimumComparablesForCondition)
        {
            comparablePrices = await QueryComparablePricesAsync(categoryId, condition: null, cancellationToken);
        }

        if (comparablePrices.Count == 0)
        {
            return Result.Failure<PriceSuggestion>(AiErrors.NotEnoughComparableData);
        }

        var min = comparablePrices.Min();
        var max = comparablePrices.Max();
        var suggested = Math.Round(comparablePrices.Average(), 0);

        return Result.Success(new PriceSuggestion(min, suggested, max, comparablePrices.Count));
    }

    private async Task<List<decimal>> QueryComparablePricesAsync(
        Guid categoryId, ListingCondition? condition, CancellationToken cancellationToken)
    {
        var query = _dbContext.Listings.Where(l =>
            l.CategoryId == categoryId
            && l.Price != null
            && (l.Status == ListingStatus.Active || l.Status == ListingStatus.ExpiringSoon || l.Status == ListingStatus.Sold));

        if (condition is { } value)
        {
            query = query.Where(l => l.Condition == value);
        }

        return await query.Select(l => l.Price!.Value).ToListAsync(cancellationToken);
    }
}
