using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Promotions.GetPromotionPlans;
using Ogasela.Domain.Promotions;
using Ogasela.Shared;

namespace Ogasela.Application.Promotions.UpdatePromotionPlan;

public sealed class UpdatePromotionPlanCommandHandler :
    IRequestHandler<UpdatePromotionPlanCommand, Result<PromotionPlanResponse>>,
    IRequestHandler<CreatePromotionPlanCommand, Result<PromotionPlanResponse>>
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

        if (await NameTakenAsync(request.Name, plan.Id, cancellationToken))
        {
            return Result.Failure<PromotionPlanResponse>(PromotionErrors.PromotionPlanNameTaken);
        }

        if (request.Price == 0m && !plan.IsFree && await AnotherFreePlanExistsAsync(plan.Id, cancellationToken))
        {
            return Result.Failure<PromotionPlanResponse>(PromotionErrors.FreePlanAlreadyExists);
        }

        var beforeJson = JsonSerializer.Serialize(plan);

        plan.Update(
            request.Name, request.Description, request.Features,
            request.DurationDays, request.PhotoLimit, request.VideoAllowed, request.BoostWeight, request.Price,
            request.AiToolTier, request.AdPlatformPushAllowed, request.BundledAdCreditKobo, request.IsActive);

        var afterJson = JsonSerializer.Serialize(plan);

        await _auditLogger.LogAsync(
            _currentUser.UserId!.Value, "PromotionPlan.Updated", nameof(PromotionPlan), plan.Id, beforeJson, afterJson,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(PromotionPlanResponse.From(plan));
    }

    public async Task<Result<PromotionPlanResponse>> Handle(
        CreatePromotionPlanCommand request, CancellationToken cancellationToken)
    {
        if (await NameTakenAsync(request.Name, null, cancellationToken))
        {
            return Result.Failure<PromotionPlanResponse>(PromotionErrors.PromotionPlanNameTaken);
        }

        if (request.Price == 0m && await AnotherFreePlanExistsAsync(null, cancellationToken))
        {
            return Result.Failure<PromotionPlanResponse>(PromotionErrors.FreePlanAlreadyExists);
        }

        var plan = PromotionPlan.Create(
            Guid.NewGuid(), request.Name, request.DurationDays, request.PhotoLimit, request.VideoAllowed,
            request.BoostWeight, request.Price, request.AiToolTier, request.AdPlatformPushAllowed,
            request.BundledAdCreditKobo, request.IsActive, request.Description, request.Features);
        _dbContext.PromotionPlans.Add(plan);

        await _auditLogger.LogAsync(
            _currentUser.UserId!.Value, "PromotionPlan.Created", nameof(PromotionPlan), plan.Id, null,
            JsonSerializer.Serialize(plan), cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(PromotionPlanResponse.From(plan));
    }

    private Task<bool> NameTakenAsync(string name, Guid? exceptId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim().ToLower();
        return _dbContext.PromotionPlans.AnyAsync(
            p => p.Name.ToLower() == normalized && (exceptId == null || p.Id != exceptId), cancellationToken);
    }

    // The Free-plan rules (category eligibility, active-ad cap) key off a single ₦0 plan.
    private Task<bool> AnotherFreePlanExistsAsync(Guid? exceptId, CancellationToken cancellationToken) =>
        _dbContext.PromotionPlans.AnyAsync(p => p.Price == 0m && (exceptId == null || p.Id != exceptId), cancellationToken);
}
