using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Analytics;

namespace Ogasela.Infrastructure.Analytics.Configurations;

public sealed class ListingDailyStatConfiguration : IEntityTypeConfiguration<ListingDailyStat>
{
    public void Configure(EntityTypeBuilder<ListingDailyStat> builder)
    {
        builder.ToTable("ListingDailyStats");
        builder.HasKey(s => new { s.ListingId, s.Date });
        builder.HasIndex(s => new { s.SellerId, s.Date });
    }
}

public sealed class SellerDailyStatConfiguration : IEntityTypeConfiguration<SellerDailyStat>
{
    public void Configure(EntityTypeBuilder<SellerDailyStat> builder)
    {
        builder.ToTable("SellerDailyStats");
        builder.HasKey(s => new { s.SellerId, s.Date });
    }
}
