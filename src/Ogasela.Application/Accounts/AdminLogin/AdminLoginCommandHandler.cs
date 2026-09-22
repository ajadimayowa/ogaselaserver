using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Notifications;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.AdminLogin;

/// <summary>
/// Step 1 of the admin portal's two-factor login. Deliberately collapses "no such account",
/// "wrong password", and "not an internal role" into the same InvalidCredentials error - an
/// attacker probing this endpoint learns nothing about which case they hit. Reuses Phase 1's
/// IOtpService/IOtpStore as-is (keyed by the account's own phone number, exactly as
/// RequestOtpCommand already does for registration) rather than standing up parallel OTP
/// infrastructure for this one flow.
/// </summary>
public sealed class AdminLoginCommandHandler : IRequestHandler<AdminLoginCommand, Result<AdminLoginResponse>>
{
    private static readonly HashSet<UserRole> AllowedRoles = [UserRole.Moderator, UserRole.FinanceAdmin, UserRole.SuperAdmin];

    private readonly IApplicationDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOtpService _otpService;
    private readonly ISmsSender _smsSender;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<AdminLoginCommandHandler> _logger;

    public AdminLoginCommandHandler(
        IApplicationDbContext dbContext,
        IPasswordHasher passwordHasher,
        IOtpService otpService,
        ISmsSender smsSender,
        IEmailSender emailSender,
        ILogger<AdminLoginCommandHandler> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _otpService = otpService;
        _smsSender = smsSender;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<AdminLoginResponse>> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash) || !AllowedRoles.Contains(user.Role))
        {
            return Result.Failure<AdminLoginResponse>(AccountErrors.InvalidCredentials);
        }

        if (string.IsNullOrWhiteSpace(user.Phone))
        {
            return Result.Failure<AdminLoginResponse>(AccountErrors.MfaPhoneRequired);
        }

        var code = await _otpService.GenerateAsync(user.Phone, cancellationToken);

        try
        {
            await _smsSender.SendAsync(user.Phone, $"Your Ogasela admin login code is {code}. It expires in 5 minutes. Do not share this code with anyone.", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send admin login OTP SMS to {Phone}", user.Phone);
        }

        try
        {
            var html = BrandedEmailTemplate.Render(
                previewText: "Your Ogasela admin login code",
                heading: "Your login code",
                bodyParagraphs:
                [
                    $"Your Ogasela admin login code is {code}. It expires in 5 minutes and can be used once.",
                    "If you didn't try to sign in, you can ignore this email - your account is still safe."
                ],
                eyebrow: "Security");

            await _emailSender.SendAsync(
                new EmailMessage(user.Email!, user.Name, "Your Ogasela admin login code", html), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send admin login OTP email to {Email}", user.Email);
        }

        return Result.Success(new AdminLoginResponse(MaskEmail(user.Email!), MaskPhone(user.Phone)));
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        return at <= 1 ? email : $"{email[0]}***{email[at..]}";
    }

    private static string MaskPhone(string phone) =>
        phone.Length <= 6 ? phone : $"{phone[..3]}***{phone[^3..]}";
}
