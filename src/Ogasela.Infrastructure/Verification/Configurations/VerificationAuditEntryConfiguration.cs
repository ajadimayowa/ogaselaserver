using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Verification;

namespace Ogasela.Infrastructure.Verification.Configurations;

public sealed class VerificationAuditEntryConfiguration : IEntityTypeConfiguration<VerificationAuditEntry>
{
    public void Configure(EntityTypeBuilder<VerificationAuditEntry> builder)
    {
        builder.ToTable("VerificationAuditEntries");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Decision).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(2000);
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasIndex(a => a.BiometricVerificationId);
    }
}
