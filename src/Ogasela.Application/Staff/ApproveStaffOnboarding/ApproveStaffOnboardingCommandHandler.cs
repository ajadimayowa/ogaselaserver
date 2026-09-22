using System.Security.Cryptography;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Notifications;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Application.Rbac;
using Ogasela.Domain.Staff;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.ApproveStaffOnboarding;

/// <summary>
/// Re-checks the assign hierarchy against the StaffProfile's existing Role at approval time (the
/// same rule CreateStaffCommandHandler enforces at creation, and the same re-check pattern
/// UpdateRoleCommandHandler applies on edit) - an HR-tier actor shouldn't be able to approve a
/// role type only an Admin could have created, even if HR didn't create this particular profile.
/// Generates a one-time temporary password, emails it, and forces a change on first login
/// (User.MustChangePassword) - the email send is best-effort: the account must still become
/// usable even if the email bounces.
/// </summary>
public sealed class ApproveStaffOnboardingCommandHandler : IRequestHandler<ApproveStaffOnboardingCommand, Result>
{
    private const string PasswordCharset = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
    private const int PasswordLength = 16;

    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailSender _emailSender;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<ApproveStaffOnboardingCommandHandler> _logger;

    public ApproveStaffOnboardingCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IDateTime dateTime,
        IPasswordHasher passwordHasher,
        IEmailSender emailSender,
        IAuditLogger auditLogger,
        ILogger<ApproveStaffOnboardingCommandHandler> logger)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task<Result> Handle(ApproveStaffOnboardingCommand request, CancellationToken cancellationToken)
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

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == staffProfile.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(StaffErrors.UserNotFound);
        }

        var plainPassword = RandomNumberGenerator.GetString(PasswordCharset, PasswordLength);
        user.SetPasswordHash(_passwordHasher.Hash(plainPassword), mustChangePassword: true);

        var now = _dateTime.UtcNow;
        var beforeJson = JsonSerializer.Serialize(new { staffProfile.Status });

        staffProfile.Approve(_currentUser.UserId.Value, now);

        await _auditLogger.LogAsync(
            _currentUser.UserId.Value,
            "Staff.Approve",
            nameof(StaffProfile),
            staffProfile.Id,
            beforeJson,
            JsonSerializer.Serialize(new { staffProfile.Status }),
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            try
            {
                var html = BrandedEmailTemplate.Render(
                    previewText: "Your Ogasela staff account is ready",
                    heading: "Your Ogasela staff account is ready",
                    bodyParagraphs:
                    [
                        $"An account has been created for you on the Ogasela Control Portal. Your temporary password is: {plainPassword}",
                        "For security, you'll be asked to set a new password the first time you sign in.",
                        "If you weren't expecting this, contact your administrator."
                    ],
                    eyebrow: "Welcome");

                await _emailSender.SendAsync(
                    new EmailMessage(user.Email, user.Name, "Your Ogasela staff account is ready", html), cancellationToken);
            }
            catch (Exception ex)
            {
                // The account is already approved and usable - a failed credentials email
                // shouldn't undo that; an admin can always reset the password another way.
                _logger.LogError(ex, "Failed to send staff onboarding credentials email to {Email}", user.Email);
            }
        }

        return Result.Success();
    }
}
