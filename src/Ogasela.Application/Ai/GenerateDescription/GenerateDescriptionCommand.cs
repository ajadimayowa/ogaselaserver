using MediatR;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Ai.GenerateDescription;

/// <summary>PromotionPlanId null = called while posting an ad (before a plan is chosen): offered to every seller, no tier check.</summary>
public sealed record GenerateDescriptionCommand(Guid? PromotionPlanId, string? Keywords, string? ImageRef)
    : IRequest<Result<ListingCopySuggestion>>;
