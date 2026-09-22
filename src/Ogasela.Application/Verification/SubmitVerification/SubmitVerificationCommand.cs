using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Verification.SubmitVerification;

public sealed record SubmitVerificationCommand(
    Stream SelfieImage,
    string SelfieFileName,
    string SelfieContentType,
    Stream IdPhotoImage,
    string IdPhotoFileName,
    string IdPhotoContentType,
    string LivenessSessionRef) : IRequest<Result<SubmitVerificationResponse>>;
