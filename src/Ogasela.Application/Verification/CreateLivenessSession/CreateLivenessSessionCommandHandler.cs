using MediatR;
using Ogasela.Application.Verification.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Verification.CreateLivenessSession;

public sealed class CreateLivenessSessionCommandHandler
    : IRequestHandler<CreateLivenessSessionCommand, Result<CreateLivenessSessionResponse>>
{
    private readonly IFaceVerificationProvider _faceProvider;

    public CreateLivenessSessionCommandHandler(IFaceVerificationProvider faceProvider)
    {
        _faceProvider = faceProvider;
    }

    public async Task<Result<CreateLivenessSessionResponse>> Handle(
        CreateLivenessSessionCommand request, CancellationToken cancellationToken)
    {
        var sessionRef = await _faceProvider.CreateLivenessSessionAsync(cancellationToken);
        return Result.Success(new CreateLivenessSessionResponse(sessionRef));
    }
}
