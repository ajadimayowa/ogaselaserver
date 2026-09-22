using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.AdIntegrations;

namespace Ogasela.Infrastructure.AdIntegrations.Configurations;

public sealed class AdAccountConnectionConfiguration : IEntityTypeConfiguration<AdAccountConnection>
{
    public void Configure(EntityTypeBuilder<AdAccountConnection> builder)
    {
        builder.ToTable("AdAccountConnections");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Platform).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(c => c.EncryptedAccessToken).IsRequired();
        builder.Property(c => c.ExternalAccountId).HasMaxLength(200).IsRequired();
        builder.Property(c => c.ConnectedAt).IsRequired();

        // Not unique: a seller can revoke and later reconnect, leaving the old (now revoked)
        // row in place for history. Active-connection lookups filter on RevokedAt == null.
        builder.HasIndex(c => new { c.SellerId, c.Platform });
    }
}
