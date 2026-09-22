using AccountVerificationStatus = Ogasela.Domain.Accounts.VerificationStatus;

namespace Ogasela.Application.Verification.GetVerificationStatus;

public sealed record VerificationStatusResponse(
    AccountVerificationStatus Status,
    int AttemptsUsed,
    int MaxAttempts,
    Guid? LatestVerificationId,
    DateTime? LatestSubmittedAt,
    DateTime? LatestDecidedAt,
    DateTime? VerifiedAt);
