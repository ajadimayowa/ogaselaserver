using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Verification.GetVerificationStatus;

public sealed record GetVerificationStatusQuery : IRequest<Result<VerificationStatusResponse>>;
