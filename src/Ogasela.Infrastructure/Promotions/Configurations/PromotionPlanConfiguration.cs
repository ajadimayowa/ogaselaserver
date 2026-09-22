using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ogasela.Domain.Promotions;
using Ogasela.Infrastructure.Promotions.Seed;

namespace Ogasela.Infrastructure.Promotions.Configurations;

public sealed class PromotionPlanConfiguration : IEntityTypeConfiguration<PromotionPlan>
{
    public void Configure(EntityTypeBuilder<PromotionPlan> builder)
    {
        builder.ToTable("PromotionPlans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.AiToolTier).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(p => p.Name).IsUnique();

        builder.HasData(
            new
            {
                Id = PromotionPlanSeedData.FreeId,
                Name = PromotionPlanName.Free,
                DurationDays = 7,
                PhotoLimit = 5,
                VideoAllowed = false,
                BoostWeight = 0,
                Price = 0m,
                AiToolTier = AiToolTier.Basic,
                AdPlatformPushAllowed = false,
                BundledAdCreditKobo = 0,
                IsActive = true
            },
            new
            {
                Id = PromotionPlanSeedData.BasicId,
                Name = PromotionPlanName.Basic,
                DurationDays = 15,
                PhotoLimit = 8,
                VideoAllowed = false,
                BoostWeight = 1,
                Price = 75000m,
                AiToolTier = AiToolTier.Standard,
                AdPlatformPushAllowed = true,
                BundledAdCreditKobo = 0,
                IsActive = true
            },
            new
            {
                Id = PromotionPlanSeedData.StandardId,
                Name = PromotionPlanName.Standard,
                DurationDays = 21,
                PhotoLimit = 10,
                VideoAllowed = true,
                BoostWeight = 2,
                Price = 200000m,
                AiToolTier = AiToolTier.Full,
                AdPlatformPushAllowed = true,
                BundledAdCreditKobo = 0,
                IsActive = true
            },
            new
            {
                Id = PromotionPlanSeedData.PremiumId,
                Name = PromotionPlanName.Premium,
                DurationDays = 30,
                PhotoLimit = 12,
                VideoAllowed = true,
                BoostWeight = 3,
                Price = 400000m,
                AiToolTier = AiToolTier.Full,
                AdPlatformPushAllowed = true,
                BundledAdCreditKobo = 200000,
                IsActive = true
            });
    }
}
