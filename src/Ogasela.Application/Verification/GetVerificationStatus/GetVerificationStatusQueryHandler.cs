using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Verification.GetVerificationStatus;

public sealed class GetVerificationStatusQueryHandler
    : IRequestHandler<GetVerificationStatusQuery, Result<VerificationStatusResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly VerificationSettings _settings;

    public GetVerificationStatusQueryHandler(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, IOptions<VerificationSettings> settings)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _settings = settings.Value;
    }

    public async Task<Result<VerificationStatusResponse>> Handle(
        GetVerificationStatusQuery request, CancellationToken cancellationToken)
    {
        var sellerProfile = await _dbContext.SellerProfiles
            .FirstOrDefaultAsync(s => s.UserId == _currentUser.UserId!.Value, cancellationToken);

        if (sellerProfile is null)
        {
            return Result.Failure<VerificationStatusResponse>(VerificationErrors.SellerProfileNotFound);
        }

        var attempts = await _dbContext.BiometricVerifications
            .Where(v => v.SellerId == sellerProfile.Id)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);

        var latest = attempts.FirstOrDefault();

        return Result.Success(new VerificationStatusResponse(
            sellerProfile.VerificationStatus,
            attempts.Count,
            _settings.MaxAttempts,
            latest?.Id,
            latest?.CreatedAt,
            latest?.DecidedAt,
            sellerProfile.VerifiedAt));
    }
}
