using MediatR;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Domain.Listings;
using Ogasela.Shared;

namespace Ogasela.Application.Ai.SuggestPrice;

public sealed record SuggestPriceCommand(Guid PromotionPlanId, Guid CategoryId, string Title, ListingCondition Condition)
    : IRequest<Result<PriceSuggestion>>;
