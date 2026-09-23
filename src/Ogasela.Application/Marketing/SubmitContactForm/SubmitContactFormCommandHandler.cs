using MediatR;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Marketing.Interfaces;
using Ogasela.Domain.Marketing;
using Ogasela.Shared;

namespace Ogasela.Application.Marketing.SubmitContactForm;

public sealed class SubmitContactFormCommandHandler : IRequestHandler<SubmitContactFormCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ITurnstileVerifier _turnstileVerifier;
    private readonly IDateTime _dateTime;

    public SubmitContactFormCommandHandler(
        IApplicationDbContext dbContext, ITurnstileVerifier turnstileVerifier, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _turnstileVerifier = turnstileVerifier;
        _dateTime = dateTime;
    }

    public async Task<Result> Handle(SubmitContactFormCommand request, CancellationToken cancellationToken)
    {
        var verified = await _turnstileVerifier.VerifyAsync(request.TurnstileToken, cancellationToken);
        if (!verified)
        {
            return Result.Failure(MarketingErrors.TurnstileVerificationFailed);
        }

        var submission = ContactFormSubmission.Create(
            request.Name, request.Email, request.Subject, request.Message, _dateTime.UtcNow);
        _dbContext.ContactFormSubmissions.Add(submission);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
