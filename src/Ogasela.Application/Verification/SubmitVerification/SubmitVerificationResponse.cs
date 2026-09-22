using Ogasela.Domain.Verification;

namespace Ogasela.Application.Verification.SubmitVerification;

public sealed record SubmitVerificationResponse(
    Guid VerificationId,
    VerificationDecision Decision,
    int AttemptNumber,
    int MaxAttempts);
