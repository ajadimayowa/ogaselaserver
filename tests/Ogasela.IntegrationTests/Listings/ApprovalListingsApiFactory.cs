using Microsoft.AspNetCore.Hosting;

namespace Ogasela.IntegrationTests.Listings;

/// <summary>Same wiring as <see cref="ListingsApiFactory"/>, with the production default restored: publishing waits for moderator approval.</summary>
public sealed class ApprovalListingsApiFactory : ListingsApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Listings:RequireApproval", "true");
    }
}
