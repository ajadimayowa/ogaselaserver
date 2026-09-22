using MediatR;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Ai.GenerateDescription;

public sealed class GenerateDescriptionCommandHandler
    : IRequestHandler<GenerateDescriptionCommand, Result<ListingCopySuggestion>>
{
    private readonly AiAccessGuard _accessGuard;
    private readonly IListingCopyGenerator _copyGenerator;

    public GenerateDescriptionCommandHandler(AiAccessGuard accessGuard, IListingCopyGenerator copyGenerator)
    {
        _accessGuard = accessGuard;
        _copyGenerator = copyGenerator;
    }

    public async Task<Result<ListingCopySuggestion>> Handle(GenerateDescriptionCommand request, CancellationToken cancellationToken)
    {
        var accessResult = await _accessGuard.RequireCapabilityAsync(
            request.PromotionPlanId, AiCapability.DescriptionGeneration, cancellationToken);

        if (accessResult.IsFailure)
        {
            return Result.Failure<ListingCopySuggestion>(accessResult.Error);
        }

        return await _copyGenerator.GenerateDescriptionAsync(request.Keywords, request.ImageRef, cancellationToken);
    }
}
