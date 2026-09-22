using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Search;

public sealed record SearchListingsQuery(SearchQuery Criteria) : IRequest<Result<SearchResultPage>>;
