using Ogasela.Application.Ai;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Shared;

namespace Ogasela.UnitTests.AdIntegrations;

/// <summary>Always fails - the plan-gating tests supply their own AdCopyTitle/Description so this path is never exercised, but PromoteListingCommandHandler still needs an IListingCopyGenerator to construct.</summary>
public sealed class FakeListingCopyGenerator : IListingCopyGenerator
{
    public Task<Result<ListingCopySuggestion>> GenerateDescriptionAsync(string? keywords, string? imageRef, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Failure<ListingCopySuggestion>(AiErrors.GenerationFailed));
}
