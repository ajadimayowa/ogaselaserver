using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Notifications;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.RequestPasswordReset;

/// <summary>
/// Sends a reset code to the chosen email or phone - but only if it belongs to a Buyer/Seller
/// account. Returns success either way, so this unauthenticated endpoint can't be used to find out
/// which emails or numbers have accounts. Staff reset through admin/password-reset instead.
/// </summary>
public sealed class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Result>
{
    private static readonly HashSet<UserRole> ConsumerRoles = [UserRole.Buyer, UserRole.Seller];

    private readonly IApplicationDbContext _dbContext;
    private readonly IOtpService _otpService;
    private readonly ISmsSender _smsSender;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

    public RequestPasswordResetCommandHandler(
        IApplicationDbContext dbContext,
        IOtpService otpService,
        ISmsSender smsSender,
        IEmailSender emailSender,
        ILogger<RequestPasswordResetCommandHandler> logger)
    {
        _dbContext = dbContext;
        _otpService = otpService;
        _smsSender = smsSender;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        if (request.Channel == PasswordResetChannel.Email)
        {
            var email = request.Email!.Trim().ToLowerInvariant();
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email, cancellationToken);

            if (user is not null && ConsumerRoles.Contains(user.Role))
            {
                var code = await _otpService.GenerateAsync(PasswordResetOtpKey.ForEmail(email), cancellationToken);
                await SendEmailAsync(user, code, cancellationToken);
            }
            else
            {
                // Not an error (the response stays the same), but makes "the email never came" easy
                // to explain from the logs: no account, or a staff account that resets via the portal.
                _logger.LogInformation(
                    "Password reset by email not sent: {Reason}",
                    user is null ? "no account with that email" : $"{user.Role} account - staff reset through the admin portal");
            }

            return Result.Success();
        }

        var phone = PasswordResetOtpKey.ForPhone(request.Phone!);
        var phoneUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Phone == phone, cancellationToken);

        if (phoneUser is not null && ConsumerRoles.Contains(phoneUser.Role))
        {
            var code = await _otpService.GenerateAsync(phone, cancellationToken);
            try
            {
                await _smsSender.SendAsync(
                    phone,
                    $"Your Ogasela password reset code is {code}. It expires in 5 minutes. Do not share this code with anyone.",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset SMS to {Phone}", phone);
            }
        }
        else
        {
            _logger.LogInformation(
                "Password reset by SMS not sent: {Reason}",
                phoneUser is null ? "no account with that phone" : $"{phoneUser.Role} account - staff reset through the admin portal");
        }

        return Result.Success();
    }

    private async Task SendEmailAsync(User user, string code, CancellationToken cancellationToken)
    {
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
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
        }
    }
}
