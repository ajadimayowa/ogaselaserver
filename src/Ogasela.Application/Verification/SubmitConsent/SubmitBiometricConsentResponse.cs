namespace Ogasela.Application.Verification.SubmitConsent;

public sealed record SubmitBiometricConsentResponse(Guid ConsentId, string ConsentVersion, DateTime AcceptedAt);
