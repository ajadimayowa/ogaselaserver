using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Moderation;

namespace Ogasela.Infrastructure.Moderation.Configurations;

public sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TargetType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(r => r.FraudScore).HasPrecision(5, 4);
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.HasIndex(r => new { r.TargetType, r.TargetId });
        builder.HasIndex(r => r.ReporterId);
    }
}
