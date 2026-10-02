using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Notifications;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.RequestAdminPasswordReset;

/// <summary>
/// Returns success whether or not the email belongs to an internal account, and never echoes
/// masked contact details back (unlike AdminLoginCommand, which only does so after a correct
/// password) - otherwise this unauthenticated endpoint would let anyone enumerate staff emails.
/// Reuses IOtpService keyed by the account's phone, exactly as AdminLoginCommand does, and sends
/// the code to both email and phone.
/// </summary>
public sealed class RequestAdminPasswordResetCommandHandler : IRequestHandler<RequestAdminPasswordResetCommand, Result>
{
    private static readonly HashSet<UserRole> AllowedRoles = [UserRole.Moderator, UserRole.FinanceAdmin, UserRole.SuperAdmin, UserRole.Staff];

    private readonly IApplicationDbContext _dbContext;
    private readonly IOtpService _otpService;
    private readonly ISmsSender _smsSender;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<RequestAdminPasswordResetCommandHandler> _logger;

    public RequestAdminPasswordResetCommandHandler(
        IApplicationDbContext dbContext,
        IOtpService otpService,
        ISmsSender smsSender,
        IEmailSender emailSender,
        ILogger<RequestAdminPasswordResetCommandHandler> logger)
    {
        _dbContext = dbContext;
        _otpService = otpService;
        _smsSender = smsSender;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result> Handle(RequestAdminPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || !AllowedRoles.Contains(user.Role) || string.IsNullOrWhiteSpace(user.Phone))
        {
            return Result.Success();
        }

        var code = await _otpService.GenerateAsync(user.Phone, cancellationToken);

        try
        {
            await _smsSender.SendAsync(user.Phone, $"Your Ogasela password reset code is {code}. It expires in 5 minutes. Do not share this code with anyone.", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send admin password reset OTP SMS to {Phone}", user.Phone);
        }

        try
        {
            var html = BrandedEmailTemplate.Render(
                previewText: "Your Ogasela password reset code",
                heading: "Reset your password",
                bodyParagraphs:
                [
                    $"Your Ogasela password reset code is {code}. It expires in 5 minutes and can be used once.",
                    "If you didn't ask to reset your password, you can ignore this email - your password hasn't changed."
                ],
                eyebrow: "Security");

            await _emailSender.SendAsync(
                new EmailMessage(user.Email!, user.Name, "Your Ogasela password reset code", html), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send admin password reset OTP email to {Email}", user.Email);
        }

        return Result.Success();
    }
}
