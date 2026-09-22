using MediatR;
using Ogasela.Domain.Verification;
using Ogasela.Shared;

namespace Ogasela.Application.Verification.ManualReviewDecision;

public sealed record ManualReviewDecisionCommand(
    Guid VerificationId,
    VerificationDecision Decision,
    string? Notes) : IRequest<Result<ManualReviewDecisionResponse>>;
