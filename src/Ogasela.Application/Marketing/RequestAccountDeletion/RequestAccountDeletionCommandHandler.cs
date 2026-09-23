using MediatR;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Marketing.Interfaces;
using Ogasela.Domain.Marketing;
using Ogasela.Shared;

namespace Ogasela.Application.Marketing.RequestAccountDeletion;

public sealed class RequestAccountDeletionCommandHandler : IRequestHandler<RequestAccountDeletionCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITurnstileVerifier _turnstileVerifier;
    private readonly IDateTime _dateTime;

    public RequestAccountDeletionCommandHandler(
        IApplicationDbContext dbContext, ITurnstileVerifier turnstileVerifier, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _turnstileVerifier = turnstileVerifier;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(RequestAccountDeletionCommand request, CancellationToken cancellationToken)
    {
        var verified = await _turnstileVerifier.VerifyAsync(request.TurnstileToken, cancellationToken);
        if (!verified)
        {
            return Result.Failure(MarketingErrors.TurnstileVerificationFailed);
        }

        var deletionRequest = AccountDeletionRequest.Create(
            request.Email, request.Phone, request.RequestType, request.Details, _dateTime.UtcNow);
        _dbContext.AccountDeletionRequests.Add(deletionRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
