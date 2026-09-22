using FluentAssertions;
using Ogasela.Application.Ai;
using Ogasela.Domain.Promotions;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.Ai;

public class AiAccessGuardTests
{
    private static (AiAccessGuard Guard, TestApplicationDbContext DbContext, Guid BasicPlanId, Guid StandardPlanId, Guid FullPlanId)
        CreateSut()
    {
        var dbContext = TestApplicationDbContext.Create();

        var basicPlan = PromotionPlan.Create(
            Guid.NewGuid(), PromotionPlanName.Basic, 15, 8, false, 1, 75000m, AiToolTier.Basic, true, 0, true);
        var standardPlan = PromotionPlan.Create(
            Guid.NewGuid(), PromotionPlanName.Standard, 21, 10, true, 2, 200000m, AiToolTier.Standard, true, 0, true);
        var fullPlan = PromotionPlan.Create(
            Guid.NewGuid(), PromotionPlanName.Premium, 30, 12, true, 3, 400000m, AiToolTier.Full, true, 200000, true);

        dbContext.PromotionPlans.AddRange(basicPlan, standardPlan, fullPlan);
        dbContext.SaveChangesAsync(CancellationToken.None).GetAwaiter().GetResult();

        var guard = new AiAccessGuard(dbContext);

        return (guard, dbContext, basicPlan.Id, standardPlan.Id, fullPlan.Id);
    }

    [Fact]
    public async Task RequireCapabilityAsync_ForImageEnhancement_RejectsABasicTierCaller()
    {
        var (guard, _, basicPlanId, _, _) = CreateSut();

        var result = await guard.RequireCapabilityAsync(basicPlanId, AiCapability.ImageEnhancement, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AiErrors.NotIncludedInPlan.Code);
    }

    [Fact]
    public async Task RequireCapabilityAsync_ForImageEnhancement_AllowsAFullTierCaller()
    {
        var (guard, _, _, _, fullPlanId) = CreateSut();

        var result = await guard.RequireCapabilityAsync(fullPlanId, AiCapability.ImageEnhancement, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RequireCapabilityAsync_ForDescriptionGeneration_AllowsEveryTierIncludingBasic()
    {
        var (guard, _, basicPlanId, standardPlanId, fullPlanId) = CreateSut();

        (await guard.RequireCapabilityAsync(basicPlanId, AiCapability.DescriptionGeneration, CancellationToken.None))
            .IsSuccess.Should().BeTrue();
        (await guard.RequireCapabilityAsync(standardPlanId, AiCapability.DescriptionGeneration, CancellationToken.None))
            .IsSuccess.Should().BeTrue();
        (await guard.RequireCapabilityAsync(fullPlanId, AiCapability.DescriptionGeneration, CancellationToken.None))
            .IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RequireCapabilityAsync_ForPriceSuggestion_RejectsBasicButAllowsStandardAndFull()
    {
        var (guard, _, basicPlanId, standardPlanId, fullPlanId) = CreateSut();

        var basicResult = await guard.RequireCapabilityAsync(basicPlanId, AiCapability.PriceSuggestion, CancellationToken.None);
        basicResult.IsFailure.Should().BeTrue();
        basicResult.Error.Code.Should().Be(AiErrors.NotIncludedInPlan.Code);

        (await guard.RequireCapabilityAsync(standardPlanId, AiCapability.PriceSuggestion, CancellationToken.None))
            .IsSuccess.Should().BeTrue();
        (await guard.RequireCapabilityAsync(fullPlanId, AiCapability.PriceSuggestion, CancellationToken.None))
            .IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RequireCapabilityAsync_WithAnUnknownPlan_ReturnsPlanNotFound()
    {
        var (guard, _, _, _, _) = CreateSut();

        var result = await guard.RequireCapabilityAsync(Guid.NewGuid(), AiCapability.DescriptionGeneration, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AiErrors.PlanNotFound.Code);
    }
}
