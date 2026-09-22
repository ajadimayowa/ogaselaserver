using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Promotions.GetPromotionPlans;
using Ogasela.Domain.Promotions;
using Ogasela.Shared;

namespace Ogasela.Application.Promotions.UpdatePromotionPlan;

public sealed class UpdatePromotionPlanCommandHandler
    : IRequestHandler<UpdatePromotionPlanCommand, Result<PromotionPlanResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _auditLogger;

    public UpdatePromotionPlanCommandHandler(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, IAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
    }

    public async Task<Result<PromotionPlanResponse>> Handle(
        UpdatePromotionPlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _dbContext.PromotionPlans.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (plan is null)
        {
            return Result.Failure<PromotionPlanResponse>(PromotionErrors.PromotionPlanNotFound);
        }

        var beforeJson = JsonSerializer.Serialize(plan);

        plan.Update(
            request.DurationDays, request.PhotoLimit, request.VideoAllowed, request.BoostWeight, request.Price,
            request.AiToolTier, request.AdPlatformPushAllowed, request.BundledAdCreditKobo, request.IsActive);

        var afterJson = JsonSerializer.Serialize(plan);

        await _auditLogger.LogAsync(
            _currentUser.UserId!.Value, "PromotionPlan.Updated", nameof(PromotionPlan), plan.Id, beforeJson, afterJson,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new PromotionPlanResponse(
            plan.Id, plan.Name, plan.DurationDays, plan.PhotoLimit, plan.VideoAllowed, plan.BoostWeight, plan.Price,
            plan.AiToolTier, plan.AdPlatformPushAllowed, plan.BundledAdCreditKobo, plan.IsActive));
    }
}
