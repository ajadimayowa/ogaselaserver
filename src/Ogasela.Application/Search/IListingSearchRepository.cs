namespace Ogasela.Application.Search;

/// <summary>
/// Runs listing search against the Postgres full-text/trigram infrastructure set up in
/// Infrastructure (the maintained tsvector column and pg_trgm extension) - kept out of
/// <c>IApplicationDbContext</c> because the ranking/distance query it runs doesn't fit LINQ.
/// </summary>
public interface IListingSearchRepository
{
    Task<SearchResultPage> SearchAsync(SearchQuery query, CancellationToken cancellationToken);
}
