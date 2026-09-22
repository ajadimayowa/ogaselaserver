using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Verification.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Verification;

namespace Ogasela.Application.Verification;

public sealed class VerificationGuard : IVerificationGuard
{
    private readonly IApplicationDbContext _dbContext;

    public VerificationGuard(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task RequireVerifiedSellerAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var status = await _dbContext.SellerProfiles
            .Where(s => s.Id == sellerId)
            .Select(s => (VerificationStatus?)s.VerificationStatus)
            .FirstOrDefaultAsync(cancellationToken);

        if (status != VerificationStatus.Verified)
        {
            throw new SellerNotVerifiedException(sellerId);
        }
    }
}
