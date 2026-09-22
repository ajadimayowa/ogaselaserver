using Ogasela.Domain.Verification;

namespace Ogasela.Application.Verification.ManualReviewDecision;

public sealed record ManualReviewDecisionResponse(Guid VerificationId, VerificationDecision Decision, DateTime DecidedAt);
