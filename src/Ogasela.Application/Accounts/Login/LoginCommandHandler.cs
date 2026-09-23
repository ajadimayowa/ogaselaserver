using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Notifications;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.Login;

/// <summary>
/// Verifies phone-or-email + password, then - for every account reaching this point (internal
/// roles are turned away below) - sends a 6-digit OTP the same way AdminLoginCommandHandler does
/// for internal roles: to the account's phone via SMS, and to its email too if it has one, both
/// best-effort. No tokens are issued here; VerifyLoginOtpCommand does that once the code is
/// confirmed.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginOtpSentResponse>>
{
    /// <summary>These roles must go through AdminLoginCommand's email+password -> OTP flow instead - never issued a token straight off a password check, however this endpoint is called.</summary>
    private static readonly HashSet<UserRole> MfaRequiredRoles = [UserRole.Moderator, UserRole.FinanceAdmin, UserRole.SuperAdmin, UserRole.Staff];

    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOtpService _otpService;
    private readonly ISmsSender _smsSender;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IOtpService otpService,
        ISmsSender smsSender,
        IEmailSender emailSender,
        ILogger<LoginCommandHandler> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _otpService = otpService;
        _smsSender = smsSender;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<LoginOtpSentResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(
            u => (request.Phone != null && u.Phone == request.Phone) ||
                 (request.Email != null && u.Email == request.Email),
            cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Result.Failure<LoginOtpSentResponse>(AccountErrors.InvalidCredentials);
        }

        if (MfaRequiredRoles.Contains(user.Role))
        {
            return Result.Failure<LoginOtpSentResponse>(AccountErrors.MfaRequired);
        }

        if (string.IsNullOrWhiteSpace(user.Phone))
        {
            return Result.Failure<LoginOtpSentResponse>(AccountErrors.MfaPhoneRequired);
        }

        var code = await _otpService.GenerateAsync(user.Phone, cancellationToken);

        try
        {
            await _smsSender.SendAsync(user.Phone, $"Your Ogasela login code is {code}. It expires in 5 minutes. Do not share this code with anyone.", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send login OTP SMS to {Phone}", user.Phone);
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            try
            {
                var html = BrandedEmailTemplate.Render(
                    previewText: "Your Ogasela login code",
                    heading: "Your login code",
                    bodyParagraphs:
                    [
                        $"Your Ogasela login code is {code}. It expires in 5 minutes and can be used once.",
                        "If you didn't try to sign in, you can ignore this email - your account is still safe."
                    ],
                    eyebrow: "Security");

                await _emailSender.SendAsync(
                    new EmailMessage(user.Email, user.Name, "Your Ogasela login code", html), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send login OTP email to {Email}", user.Email);
            }
        }

        return Result.Success(new LoginOtpSentResponse(MaskEmail(user.Email), MaskPhone(user.Phone)));
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return string.Empty;
        }

        var at = email.IndexOf('@');
        return at <= 1 ? email : $"{email[0]}***{email[at..]}";
    }

    private static string MaskPhone(string phone) =>
        phone.Length <= 6 ? phone : $"{phone[..3]}***{phone[^3..]}";
}
