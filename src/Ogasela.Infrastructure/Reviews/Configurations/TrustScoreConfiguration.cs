using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Reviews;

namespace Ogasela.Infrastructure.Reviews.Configurations;

public sealed class TrustScoreConfiguration : IEntityTypeConfiguration<TrustScore>
{
    public void Configure(EntityTypeBuilder<TrustScore> builder)
    {
        builder.ToTable("TrustScores");

        builder.HasKey(t => t.SellerId);

        builder.Property(t => t.Score).HasPrecision(5, 2).IsRequired();
        builder.Property(t => t.LastComputedAt).IsRequired();
    }
}
