using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Accounts;
using Ogasela.Api.RateLimiting;
using Ogasela.Application.Accounts.AdminLogin;
using Ogasela.Application.Accounts.ChangePassword;
using Ogasela.Application.Accounts.ConfirmPasswordReset;
using Ogasela.Application.Accounts.Login;
using Ogasela.Application.Accounts.Logout;
using Ogasela.Application.Accounts.Refresh;
using Ogasela.Application.Accounts.RegisterUser;
using Ogasela.Application.Accounts.RequestOtp;
using Ogasela.Application.Accounts.VerifyAdminLoginOtp;
using Ogasela.Application.Accounts.VerifyOtp;

namespace Ogasela.Api.Controllers.Accounts;

/// <summary>Every action here is unauthenticated by nature (register/login/OTP/refresh/logout all happen before or without a valid access token), so the whole controller sits behind the "auth" rate-limit policy - see RateLimitingExtensions.</summary>
[ApiController]
[Route("api/v1/auth")]
[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Creates a Buyer or Seller account (AccountType must be one of those two - internal roles have no public signup path). BusinessName is required when AccountType is Seller.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.Phone,
            request.Email,
            request.Password,
            request.AccountType,
            request.BusinessName,
            request.RcNumber,
            request.Nin);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Sends a 6-digit SMS verification code to the given phone number, valid for 5 minutes.</summary>
    [HttpPost("otp/request")]
    public async Task<IActionResult> RequestOtp(RequestOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RequestOtpCommand(request.Phone), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Verifies an OTP code sent via otp/request. Limited to 5 attempts per phone number per 15 minutes regardless of this route's own rate limit.</summary>
    [HttpPost("otp/verify")]
    public async Task<IActionResult> VerifyOtp(VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new VerifyOtpCommand(request.Phone, request.Code), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Completes a password reset: request a code first via otp/request (same endpoint used for
    /// any other phone verification - nothing ties it to "reset" specifically), then call this
    /// with that code and the new password. No prior authentication needed - having the code at
    /// all already proves phone ownership.
    /// </summary>
    [HttpPost("password-reset/confirm")]
    public async Task<IActionResult> ConfirmPasswordReset(ConfirmPasswordResetRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ConfirmPasswordResetCommand(request.Phone, request.Code, request.NewPassword), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Exchanges phone-or-email + password for a fresh access/refresh token pair. Exactly one of Phone/Email must match an existing account.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new LoginCommand(request.Phone, request.Email, request.Password), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Step 1 of the admin portal's two-factor login: email + password. Internal roles
    /// (Moderator/FinanceAdmin/SuperAdmin) cannot use the plain login endpoint above at all - it
    /// always fails with User.MfaRequired for them - so this is the only way in. On success, an
    /// OTP has been sent to the account's email and phone; complete sign-in with admin/login/verify.
    /// </summary>
    [HttpPost("admin/login")]
    public async Task<IActionResult> AdminLogin(AdminLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AdminLoginCommand(request.Email, request.Password), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Step 2: the OTP admin/login sent. No password here - having a code to submit at all already proves step 1 passed.</summary>
    [HttpPost("admin/login/verify")]
    public async Task<IActionResult> VerifyAdminLoginOtp(VerifyAdminLoginOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new VerifyAdminLoginOtpCommand(request.Email, request.Code), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Rotates a still-valid refresh token for a new access/refresh token pair. Presenting an already-rotated or revoked token is treated as token reuse and invalidates the whole token family.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Revokes the given refresh token, ending that session. The (still-valid, short-lived) access token itself keeps working until it naturally expires.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Changes the caller's own password. Unlike every other action on this controller, this one
    /// needs a signed-in user - the class carries no [Authorize] itself (every other action here
    /// is intentionally anonymous), so it's added explicitly just on this action.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ChangePasswordCommand(request.CurrentPassword, request.NewPassword), cancellationToken);
        return result.ToActionResult(this);
    }
}
