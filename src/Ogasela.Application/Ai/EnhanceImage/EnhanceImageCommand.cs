using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Ai.EnhanceImage;

public sealed record EnhanceImageCommand(
    Guid PromotionPlanId, Stream Image, string FileName, string ContentType) : IRequest<Result<EnhanceImageResponse>>;

public sealed record EnhanceImageResponse(string EnhancedImageKey, string EnhancedImageUrl);
