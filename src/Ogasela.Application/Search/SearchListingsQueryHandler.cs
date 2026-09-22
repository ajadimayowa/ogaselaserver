using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Search;

public sealed class SearchListingsQueryHandler : IRequestHandler<SearchListingsQuery, Result<SearchResultPage>>
{
    private readonly IListingSearchRepository _searchRepository;

    public SearchListingsQueryHandler(IListingSearchRepository searchRepository)
    {
        _searchRepository = searchRepository;
    }

    public async Task<Result<SearchResultPage>> Handle(SearchListingsQuery request, CancellationToken cancellationToken)
    {
        var page = await _searchRepository.SearchAsync(request.Criteria, cancellationToken);
        return Result.Success(page);
    }
}
