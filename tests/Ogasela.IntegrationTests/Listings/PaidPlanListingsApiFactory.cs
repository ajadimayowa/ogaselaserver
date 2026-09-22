using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ogasela.Application.Payments;

namespace Ogasela.IntegrationTests.Listings;

/// <summary>
/// Same wiring as <see cref="ListingsApiFactory"/>, except <c>IPaymentAuthorizer</c> is swapped
/// for one that always authorizes - needed to test that a successful payment actually lets a
/// paid plan publish, since the real StubPaymentAuthorizer always fails paid plans by design.
/// </summary>
public sealed class PaidPlanListingsApiFactory : ListingsApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPaymentAuthorizer>();
            services.AddScoped<IPaymentAuthorizer, AlwaysSucceedPaymentAuthorizer>();
        });
    }
}
