using MediatR;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Ai.GenerateDescription;

public sealed record GenerateDescriptionCommand(Guid PromotionPlanId, string? Keywords, string? ImageRef)
    : IRequest<Result<ListingCopySuggestion>>;
