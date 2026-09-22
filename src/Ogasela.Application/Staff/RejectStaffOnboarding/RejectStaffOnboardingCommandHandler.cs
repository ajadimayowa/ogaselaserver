using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Rbac;
using Ogasela.Domain.Staff;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.RejectStaffOnboarding;

/// <summary>Same hierarchy re-check as ApproveStaffOnboardingCommandHandler - see its doc comment. No email is sent for a rejection.</summary>
public sealed class RejectStaffOnboardingCommandHandler : IRequestHandler<RejectStaffOnboardingCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;
    private readonly IAuditLogger _auditLogger;

    public RejectStaffOnboardingCommandHandler(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, IDateTime dateTime, IAuditLogger auditLogger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _auditLogger = auditLogger;
    }

    public async Task<Result> Handle(RejectStaffOnboardingCommand request, CancellationToken cancellationToken)
    {
        var staffProfile = await _dbContext.StaffProfiles
            .FirstOrDefaultAsync(s => s.Id == request.StaffProfileId, cancellationToken);

        if (staffProfile is null)
        {
            return Result.Failure(StaffErrors.StaffProfileNotFound);
        }

        if (staffProfile.Status != StaffOnboardingStatus.PendingApproval)
        {
            return Result.Failure(StaffErrors.AlreadyDecided);
        }

        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Id == staffProfile.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(StaffErrors.RoleNotFound);
        }

        var actorTier = await StaffHierarchy.GetActorTierAsync(_dbContext, _currentUser.UserId!.Value, cancellationToken);
        if (actorTier is null)
        {
            return Result.Failure(StaffErrors.ActorNotEligible);
        }

        if (!StaffHierarchy.CanAssign(actorTier.Value, role.RoleType))
        {
            return Result.Failure(StaffErrors.InsufficientHierarchyPermission);
        }

        var beforeJson = JsonSerializer.Serialize(new { staffProfile.Status });

        staffProfile.Reject(_currentUser.UserId.Value, _dateTime.UtcNow, request.Reason);

        await _auditLogger.LogAsync(
            _currentUser.UserId.Value,
            "Staff.Reject",
            nameof(StaffProfile),
            staffProfile.Id,
            beforeJson,
            JsonSerializer.Serialize(new { staffProfile.Status, staffProfile.RejectionReason }),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
