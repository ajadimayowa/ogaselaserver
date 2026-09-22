using MediatR;
using Ogasela.Application.Ai.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Ai.EnhanceImage;

public sealed class EnhanceImageCommandHandler : IRequestHandler<EnhanceImageCommand, Result<EnhanceImageResponse>>
{
    private readonly AiAccessGuard _accessGuard;
    private readonly IAiImageStorage _imageStorage;
    private readonly IImageEnhancer _imageEnhancer;

    public EnhanceImageCommandHandler(AiAccessGuard accessGuard, IAiImageStorage imageStorage, IImageEnhancer imageEnhancer)
    {
        _accessGuard = accessGuard;
        _imageStorage = imageStorage;
        _imageEnhancer = imageEnhancer;
    }

    public async Task<Result<EnhanceImageResponse>> Handle(EnhanceImageCommand request, CancellationToken cancellationToken)
    {
        var accessResult = await _accessGuard.RequireCapabilityAsync(
            request.PromotionPlanId, AiCapability.ImageEnhancement, cancellationToken);

        if (accessResult.IsFailure)
        {
            return Result.Failure<EnhanceImageResponse>(accessResult.Error);
        }

        var originalKey = await _imageStorage.UploadAsync(
            request.FileName, request.ContentType, request.Image, cancellationToken);

        var enhanceResult = await _imageEnhancer.EnhanceAsync(originalKey, cancellationToken);
        if (enhanceResult.IsFailure)
        {
            return Result.Failure<EnhanceImageResponse>(enhanceResult.Error);
        }

        var enhancedKey = enhanceResult.Value;
        return Result.Success(new EnhanceImageResponse(enhancedKey, _imageStorage.GetImageUrl(enhancedKey)));
    }
}
