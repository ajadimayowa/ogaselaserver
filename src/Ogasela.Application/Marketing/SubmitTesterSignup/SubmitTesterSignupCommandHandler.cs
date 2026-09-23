using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Marketing.Interfaces;
using Ogasela.Domain.Marketing;
using Ogasela.Shared;

namespace Ogasela.Application.Marketing.SubmitTesterSignup;

public sealed class SubmitTesterSignupCommandHandler : IRequestHandler<SubmitTesterSignupCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITurnstileVerifier _turnstileVerifier;
    private readonly IDateTime _dateTime;

    public SubmitTesterSignupCommandHandler(
        IApplicationDbContext dbContext, ITurnstileVerifier turnstileVerifier, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _turnstileVerifier = turnstileVerifier;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(SubmitTesterSignupCommand request, CancellationToken cancellationToken)
    {
        var verified = await _turnstileVerifier.VerifyAsync(request.TurnstileToken, cancellationToken);
        if (!verified)
        {
            return Result.Failure(MarketingErrors.TurnstileVerificationFailed);
        }

        // Signing up twice with the same email is harmless for a waitlist - treat it as success
        // rather than surfacing a confusing "already exists" error for something this low-stakes.
        var alreadySignedUp = await _dbContext.TesterSignups.AnyAsync(t => t.Email == request.Email, cancellationToken);
        if (!alreadySignedUp)
        {
            _dbContext.TesterSignups.Add(TesterSignup.Create(request.Name, request.Email, _dateTime.UtcNow));
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
