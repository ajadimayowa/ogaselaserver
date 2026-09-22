namespace Ogasela.Application.Payments;

public interface IPaymentGatewayResolver
{
    /// <summary>The gateway currently configured via <c>Payments:Provider</c>, used to start new checkouts/refunds.</summary>
    IPaymentGateway GetActive();

    /// <summary>Looks a gateway up by name - used by the webhook endpoints, which are provider-specific regardless of which one is "active".</summary>
    IPaymentGateway? GetByProviderName(string providerName);
}
