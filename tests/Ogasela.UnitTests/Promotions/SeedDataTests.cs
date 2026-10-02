using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Ogasela.Domain.Promotions;
using Ogasela.Infrastructure.Persistence;

namespace Ogasela.UnitTests.Promotions;

/// <summary>
/// Exercises the real <see cref="OgaselaDbContext"/> model (including
/// CategoryConfiguration/PromotionPlanConfiguration's HasData) against EF Core's InMemory
/// provider, which applies model-level seed data on EnsureCreated the same way a real
/// migration's InsertData would - so this validates the same seed values Postgres would end up with.
/// </summary>
public class SeedDataTests
{
    private static OgaselaDbContext CreateSeededDbContext()
    {
        var options = new DbContextOptionsBuilder<OgaselaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new OgaselaDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    [Fact]
    public void PromotionPlanSeedData_LoadsAllFourPlansWithExpectedValues()
    {
        using var dbContext = CreateSeededDbContext();

        var plans = dbContext.PromotionPlans.ToList();
        plans.Should().HaveCount(4);

        var free = plans.Single(p => p.Name == PromotionPlanName.Free);
        free.DurationDays.Should().Be(7);
        free.PhotoLimit.Should().Be(5);
        free.VideoAllowed.Should().BeFalse();
        free.BoostWeight.Should().Be(0);
        free.Price.Should().Be(0m);
        free.AiToolTier.Should().Be(AiToolTier.Basic);
        free.AdPlatformPushAllowed.Should().BeFalse();
        free.BundledAdCreditKobo.Should().Be(0);
        free.IsActive.Should().BeTrue();

        var basic = plans.Single(p => p.Name == PromotionPlanName.Basic);
        basic.DurationDays.Should().Be(15);
        basic.PhotoLimit.Should().Be(8);
        basic.VideoAllowed.Should().BeFalse();
        basic.BoostWeight.Should().Be(1);
        basic.Price.Should().Be(75000m);
        basic.AiToolTier.Should().Be(AiToolTier.Standard);
        basic.AdPlatformPushAllowed.Should().BeTrue();
        basic.BundledAdCreditKobo.Should().Be(0);

        var standard = plans.Single(p => p.Name == PromotionPlanName.Standard);
        standard.DurationDays.Should().Be(21);
        standard.PhotoLimit.Should().Be(10);
        standard.VideoAllowed.Should().BeTrue();
        standard.BoostWeight.Should().Be(2);
        standard.Price.Should().Be(200000m);
        standard.AiToolTier.Should().Be(AiToolTier.Full);
        standard.AdPlatformPushAllowed.Should().BeTrue();
        standard.BundledAdCreditKobo.Should().Be(0);

        var premium = plans.Single(p => p.Name == PromotionPlanName.Premium);
        premium.DurationDays.Should().Be(30);
        premium.PhotoLimit.Should().Be(12);
        premium.VideoAllowed.Should().BeTrue();
        premium.BoostWeight.Should().Be(3);
        premium.Price.Should().Be(400000m);
        premium.AiToolTier.Should().Be(AiToolTier.Full);
        premium.AdPlatformPushAllowed.Should().BeTrue();
        premium.BundledAdCreditKobo.Should().Be(200000);

        plans.Should().OnlyContain(p => p.IsActive);
    }

    [Fact]
    public void CategorySeedData_LoadsStarterCategoriesWithSubcategoriesAndExpectedFreeEligibility()
    {
        using var dbContext = CreateSeededDbContext();

        var all = dbContext.Categories.ToList();
        var categories = all.Where(c => c.ParentCategoryId == null).ToList();
        var subcategories = all.Where(c => c.ParentCategoryId != null).ToList();

        categories.Select(c => c.Name).Should().BeEquivalentTo(
            "Electronics", "Fashion", "Vehicles", "Real Estate", "Home & Furniture", "Jobs", "Services");

        // Every top-level category is seeded with subcategories, one level deep, each inheriting
        // its parent's free-eligibility - listings are posted under a subcategory.
        categories.Should().OnlyContain(c => subcategories.Any(s => s.ParentCategoryId == c.Id));
        subcategories.Should().OnlyContain(s => categories.Any(c => c.Id == s.ParentCategoryId));
        subcategories.Should().OnlyContain(s => s.IsFreeEligible == categories.Single(c => c.Id == s.ParentCategoryId).IsFreeEligible);

        all.Should().OnlyContain(c => c.AttributeSchemaVersion == 1);
        all.Should().OnlyContain(c => c.ImageS3Key == null, "no real images exist to seed yet");

        categories.Where(c => c.Name is "Vehicles" or "Real Estate" or "Jobs")
            .Should().OnlyContain(c => !c.IsFreeEligible);

        categories.Where(c => c.Name is "Electronics" or "Fashion" or "Home & Furniture" or "Services")
            .Should().OnlyContain(c => c.IsFreeEligible);
    }
}
