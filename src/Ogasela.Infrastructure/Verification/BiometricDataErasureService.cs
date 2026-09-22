using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Verification.Interfaces;

namespace Ogasela.Infrastructure.Verification;

/// <summary>
/// Handles a data-erasure request for a single verification record: deletes the raw S3 images
/// (if they haven't already expired), removes the corresponding Rekognition face enrollment,
/// and marks the record erased. Intended for future NDPA deletion-request support.
/// </summary>
public sealed class BiometricDataErasureService : IBiometricDataErasureService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IBiometricImageStorage _imageStorage;
    private readonly IFaceVerificationProvider _faceProvider;

    public BiometricDataErasureService(
        IApplicationDbContext dbContext, IBiometricImageStorage imageStorage, IFaceVerificationProvider faceProvider)
    {
        _dbContext = dbContext;
        _imageStorage = imageStorage;
        _faceProvider = faceProvider;
    }

    public async Task EraseAsync(Guid biometricVerificationId, CancellationToken cancellationToken)
    {
        var verification = await _dbContext.BiometricVerifications
            .FirstOrDefaultAsync(v => v.Id == biometricVerificationId, cancellationToken);

        if (verification is null || verification.IsErased)
        {
            return;
        }

        if (verification.SelfieImageS3Key is { } selfieKey)
        {
            await _imageStorage.DeleteAsync(selfieKey, cancellationToken);
        }

        if (verification.IdPhotoImageS3Key is { } idPhotoKey)
        {
            await _imageStorage.DeleteAsync(idPhotoKey, cancellationToken);
        }

        if (verification.RekognitionFaceId is { } faceId)
        {
            await _faceProvider.DeleteFaceAsync(faceId, cancellationToken);
        }

        verification.Erase();

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
