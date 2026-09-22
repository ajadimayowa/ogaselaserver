using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Verification;

namespace Ogasela.Infrastructure.Verification.Configurations;

public sealed class BiometricVerificationConfiguration : IEntityTypeConfiguration<BiometricVerification>
{
    public void Configure(EntityTypeBuilder<BiometricVerification> builder)
    {
        builder.ToTable("BiometricVerifications");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.RekognitionFaceId).HasMaxLength(200);
        builder.Property(v => v.LivenessScore).HasPrecision(5, 2);
        builder.Property(v => v.IdMatchScore).HasPrecision(5, 2);
        builder.Property(v => v.Decision).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(v => v.DecisionSource).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(v => v.SelfieImageS3Key).HasMaxLength(500);
        builder.Property(v => v.IdPhotoImageS3Key).HasMaxLength(500);
        builder.Property(v => v.CreatedAt).IsRequired();

        builder.HasIndex(v => v.SellerId);
        builder.HasIndex(v => v.RawImageExpiryDate);
    }
}
