namespace Ogasela.Application.Verification.Interfaces;

/// <summary>
/// Supports future NDPA deletion-request handling: erases a seller's raw biometric images and
/// their Rekognition face enrollment, leaving only the non-identifying decision/score history
/// behind on the (marked-erased) verification record.
/// </summary>
public interface IBiometricDataErasureService
{
    Task EraseAsync(Guid biometricVerificationId, CancellationToken cancellationToken);
}
