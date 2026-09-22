using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Verification.CreateLivenessSession;

/// <summary>
/// Starts a server-side AWS Face Liveness session and hands the session reference to the
/// client, which then runs the vendor's Face Liveness SDK against it. Not part of the phase's
/// literal endpoint list, but required plumbing: the client can't create this session itself
/// without embedding AWS credentials, and <c>SubmitVerificationCommand</c> needs a completed
/// session ref to resolve via <c>IFaceVerificationProvider.CheckLivenessAsync</c>.
/// </summary>
public sealed record CreateLivenessSessionCommand : IRequest<Result<CreateLivenessSessionResponse>>;
