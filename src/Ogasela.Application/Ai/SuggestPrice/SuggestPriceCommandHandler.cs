using MediatR;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Ai.SuggestPrice;

public sealed class SuggestPriceCommandHandler : IRequestHandler<SuggestPriceCommand, Result<PriceSuggestion>>
{
    private readonly AiAccessGuard _accessGuard;
    private readonly IPriceSuggestionService _priceSuggestionService;

    public SuggestPriceCommandHandler(AiAccessGuard accessGuard, IPriceSuggestionService priceSuggestionService)
    {
        _accessGuard = accessGuard;
        _priceSuggestionService = priceSuggestionService;
    }

    public async Task<Result<PriceSuggestion>> Handle(SuggestPriceCommand request, CancellationToken cancellationToken)
    {
        var accessResult = await _accessGuard.RequireCapabilityAsync(
            request.PromotionPlanId, AiCapability.PriceSuggestion, cancellationToken);

        if (accessResult.IsFailure)
        {
            return Result.Failure<PriceSuggestion>(accessResult.Error);
        }

        return await _priceSuggestionService.SuggestPriceRangeAsync(
            request.CategoryId, request.Title, request.Condition, cancellationToken);
    }
}
