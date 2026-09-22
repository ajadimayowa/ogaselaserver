using Ogasela.Application.Payments;
using Ogasela.Shared;

namespace Ogasela.IntegrationTests.Listings;

public sealed class AlwaysSucceedPaymentAuthorizer : IPaymentAuthorizer
{
    public Task<Result<PaymentReceipt>> AuthorizeAsync(
        Guid sellerId, Guid promotionPlanId, Guid relatedListingId, CancellationToken cancellationToken)
    {
        var receipt = new PaymentReceipt(Guid.NewGuid(), sellerId, promotionPlanId, 0m, DateTime.UtcNow);
        return Task.FromResult(Result.Success(receipt));
    }
}
