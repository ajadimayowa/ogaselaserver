using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.AdIntegrations;

namespace Ogasela.Infrastructure.AdIntegrations.Configurations;

public sealed class AdCampaignConfiguration : IEntityTypeConfiguration<AdCampaign>
{
    public void Configure(EntityTypeBuilder<AdCampaign> builder)
    {
        builder.ToTable("AdCampaigns");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Platform).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(c => c.ExternalCampaignId).HasMaxLength(200).IsRequired();
        builder.Property(c => c.BudgetKobo).HasPrecision(18, 2).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        // Looked up by (ListingId, Platform), most-recently-created row first - see
        // AdIntegrationLookups.GetLatestCampaignAsync.
        builder.HasIndex(c => new { c.ListingId, c.Platform, c.CreatedAt });

        // The metrics-sync job (every 15-30 min) only touches Active rows.
        builder.HasIndex(c => c.Status);
    }
}
