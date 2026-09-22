using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Verification;
using Ogasela.Shared;

namespace Ogasela.Application.Verification.SubmitConsent;

public sealed class SubmitBiometricConsentCommandHandler
    : IRequestHandler<SubmitBiometricConsentCommand, Result<SubmitBiometricConsentResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;
    private readonly VerificationSettings _settings;

    public SubmitBiometricConsentCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IDateTime dateTime,
        IOptions<VerificationSettings> settings)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _settings = settings.Value;
    }

    public async Task<Result<SubmitBiometricConsentResponse>> Handle(
        SubmitBiometricConsentCommand request, CancellationToken cancellationToken)
    {
        var sellerId = await _dbContext.SellerProfiles
            .Where(s => s.UserId == _currentUser.UserId!.Value)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (sellerId == Guid.Empty)
        {
            return Result.Failure<SubmitBiometricConsentResponse>(VerificationErrors.SellerProfileNotFound);
        }

        var consent = BiometricConsent.Create(
            sellerId, _settings.CurrentConsentVersion, _dateTime.UtcNow, request.IpAddress);

        _dbContext.BiometricConsents.Add(consent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(
            new SubmitBiometricConsentResponse(consent.Id, consent.ConsentVersion, consent.AcceptedAt));
    }
}
