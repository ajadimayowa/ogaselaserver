using MediatR;
using Ogasela.Application.Analytics;
using Ogasela.Shared;

namespace Ogasela.Application.Search;

public sealed class SearchListingsQueryHandler : IRequestHandler<SearchListingsQuery, Result<SearchResultPage>>
{
    private readonly IListingSearchRepository _searchRepository;
    private readonly IEngagementTracker _engagementTracker;

    public SearchListingsQueryHandler(IListingSearchRepository searchRepository, IEngagementTracker engagementTracker)
    {
        _searchRepository = searchRepository;
        _engagementTracker = engagementTracker;
    }

    public async Task<Result<SearchResultPage>> Handle(SearchListingsQuery request, CancellationToken cancellationToken)
    {
        var page = await _searchRepository.SearchAsync(request.Criteria, cancellationToken);

        // Every listing shown in a result page (search, category pages, home-screen rails) is an impression.
        await _engagementTracker.RecordAsync(
            page.Items.Select(i => (i.Id, i.SellerId)).ToList(), ListingEngagement.Impression, cancellationToken);

        return Result.Success(page);
    }
}
