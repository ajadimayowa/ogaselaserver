using Microsoft.Extensions.Options;

namespace Ogasela.Application.Payments;

public sealed class PaymentGatewayResolver : IPaymentGatewayResolver
{
    private readonly IReadOnlyList<IPaymentGateway> _gateways;
    private readonly PaymentSettings _settings;

    public PaymentGatewayResolver(IEnumerable<IPaymentGateway> gateways, IOptions<PaymentSettings> settings)
    {
        _gateways = gateways.ToList();
        _settings = settings.Value;
    }

    public IPaymentGateway GetActive()
    {
        return GetByProviderName(_settings.Provider)
            ?? throw new InvalidOperationException(
                $"No IPaymentGateway is registered for Payments:Provider '{_settings.Provider}'.");
    }

    public IPaymentGateway? GetByProviderName(string providerName) =>
        _gateways.FirstOrDefault(g => string.Equals(g.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));
}
