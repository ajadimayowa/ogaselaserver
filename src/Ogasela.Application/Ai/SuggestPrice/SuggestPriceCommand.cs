using MediatR;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Domain.Listings;
using Ogasela.Shared;

namespace Ogasela.Application.Ai.SuggestPrice;

/// <summary>PromotionPlanId null = called while posting an ad (before a plan is chosen): offered to every seller, no tier check.</summary>
public sealed record SuggestPriceCommand(Guid? PromotionPlanId, Guid CategoryId, string Title, ListingCondition Condition)
    : IRequest<Result<PriceSuggestion>>;
