using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

        builder.Property(p => p.Name).HasMaxLength(40).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(300);
        // Stored one feature per line; the comparer lets EF spot in-place edits to the list.
        builder.Property(p => p.Features)
            .HasConversion(
                v => string.Join('\n', v),
                v => v.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
                new ValueComparer<List<string>>(
                    (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
                    v => v.Aggregate(0, (hash, f) => HashCode.Combine(hash, f.GetHashCode())),
                    v => v.ToList()))
            .HasMaxLength(1000)
            .IsRequired();
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.AiToolTier).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.HasIndex(p => p.Name).IsUnique();

        // Launch plans only. SuperAdmins edit plans from the Control Portal after that, so never change
        // these values in a later migration - EF would overwrite whatever the admin has set.
        builder.HasData(
            new
            {
                Id = PromotionPlanSeedData.FreeId,
                Name = PromotionPlanName.Free,
                Description = "Get your ad in front of buyers at no cost.",
                Features = new List<string> { "Live for 7 days", "Up to 5 photos", "AI listing helper (basic)" },
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
                Description = "A longer run and a small boost in search.",
                Features = new List<string> { "Live for 15 days", "Up to 8 photos", "Small boost in search results", "Promote on TikTok and Facebook", "AI listing helper (standard)" },
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
                Description = "More photos, video and a stronger boost.",
                Features = new List<string> { "Live for 21 days", "Up to 10 photos and a video", "Stronger boost in search results", "Promote on TikTok and Facebook", "Full AI listing tools" },
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
                Description = "Maximum visibility, with ad credit included.",
                Features = new List<string> { "Live for 30 days", "Up to 12 photos and a video", "Top boost in search results", "Promote on TikTok and Facebook", "₦2,000 ad credit included", "Full AI listing tools" },
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
