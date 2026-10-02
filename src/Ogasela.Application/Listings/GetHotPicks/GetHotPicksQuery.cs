using MediatR;
using Ogasela.Application.Search;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.GetHotPicks;

/// <summary>
/// The home screen's "Hot Picks": up to <see cref="GetHotPicksQueryHandler.MaxItems"/> live ads that
/// buyers have been engaging with most, spread across categories, paged. CategoryId (optional)
/// narrows it to one category - a top-level category includes its subcategories. Location (optional)
/// keeps only ads whose location mentions that place - "Ikeja" matches "Ikeja, Lagos". Seed keeps the
/// order stable while one visitor pages through; a new seed gives a fresh mix.
/// </summary>
public sealed record GetHotPicksQuery(Guid? CategoryId, int Page, int PageSize, int Seed, string? Location = null) : IRequest<Result<SearchResultPage>>;
