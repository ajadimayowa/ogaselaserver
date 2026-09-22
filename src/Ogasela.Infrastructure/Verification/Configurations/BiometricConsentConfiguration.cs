using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Verification;

namespace Ogasela.Infrastructure.Verification.Configurations;

public sealed class BiometricConsentConfiguration : IEntityTypeConfiguration<BiometricConsent>
{
    public void Configure(EntityTypeBuilder<BiometricConsent> builder)
    {
        builder.ToTable("BiometricConsents");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ConsentVersion).HasMaxLength(20).IsRequired();
        builder.Property(c => c.IpAddress).HasMaxLength(64).IsRequired();
        builder.Property(c => c.AcceptedAt).IsRequired();

        builder.HasIndex(c => c.SellerId);
    }
}
