using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Accounts;
using Ogasela.Api.RateLimiting;
using Ogasela.Application.Accounts.AdminLogin;
using Ogasela.Application.Accounts.ChangePassword;
using Ogasela.Application.Accounts.ConfirmAdminPasswordReset;
using Ogasela.Application.Accounts.ConfirmPasswordReset;
using Ogasela.Application.Accounts.Login;
using Ogasela.Application.Accounts.Logout;
using Ogasela.Application.Accounts.Refresh;
using Ogasela.Application.Accounts.RegisterUser;
using Ogasela.Application.Accounts.RequestAdminPasswordReset;
using Ogasela.Application.Accounts.RequestOtp;
using Ogasela.Application.Accounts.RequestPasswordReset;
using Ogasela.Application.Accounts.SocialLogin;
using Ogasela.Application.Accounts.VerifyAdminLoginOtp;
using Ogasela.Application.Accounts.VerifyLoginOtp;
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
            request.Nin,
            request.Name);

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
    /// Step 1 of a password reset: sends a 6-digit code by the channel the user chose - Email
    /// (with Email) or Phone (with Phone) - if it belongs to a Buyer/Seller account. Always returns
    /// success so it can't be used to discover which emails/numbers are registered.
    /// </summary>
    [HttpPost("password-reset/request")]
    public async Task<IActionResult> RequestPasswordReset(RequestPasswordResetRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RequestPasswordResetCommand(request.Channel, request.Email, request.Phone), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Step 2: the code plus the new password, with the same Email or Phone the code went to.
    /// (A phone code from the older otp/request also works.) Signs the account out of every
    /// existing session.
    /// </summary>
    [HttpPost("password-reset/confirm")]
    public async Task<IActionResult> ConfirmPasswordReset(ConfirmPasswordResetRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ConfirmPasswordResetCommand(request.Phone, request.Code, request.NewPassword, request.Email), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Step 1 of every consumer login: phone-or-email + password. Exactly one of Phone/Email must
    /// match an existing account. On success an OTP has been sent to the account's phone (and
    /// email, if it has one); complete sign-in with login/verify. Internal roles
    /// (Moderator/FinanceAdmin/SuperAdmin/Staff) cannot use this endpoint at all - it always fails
    /// with User.MfaRequired for them - see admin/login instead.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new LoginCommand(request.Phone, request.Email, request.Password), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Step 2: the OTP login sent. No password here - having a code to submit at all already proves step 1 passed. Phone/Email should match whichever identifier was used for login.</summary>
    [HttpPost("login/verify")]
    public async Task<IActionResult> VerifyLoginOtp(VerifyLoginOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new VerifyLoginOtpCommand(request.Phone, request.Email, request.Code), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Step 1 of the admin portal's two-factor login: email + password. Internal roles
    /// (Moderator/FinanceAdmin/SuperAdmin/Staff) cannot use the plain login endpoint above at all
    /// - it always fails with User.MfaRequired for them - so this is the only way in. On success,
    /// an OTP has been sent to the account's email and phone; complete sign-in with admin/login/verify.
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

    /// <summary>
    /// Step 1 of the admin portal's "forgot password" flow: email only. If it belongs to an
    /// internal account, a reset code is sent to that account's email and phone. Always returns
    /// success either way so the endpoint can't be used to discover which emails are staff
    /// accounts. Complete with admin/password-reset/confirm.
    /// </summary>
    [HttpPost("admin/password-reset/request")]
    public async Task<IActionResult> RequestAdminPasswordReset(RequestAdminPasswordResetRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RequestAdminPasswordResetCommand(request.Email), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>Step 2: the code admin/password-reset/request sent, plus the new password. Signs the account out of every existing session.</summary>
    [HttpPost("admin/password-reset/confirm")]
    public async Task<IActionResult> ConfirmAdminPasswordReset(ConfirmAdminPasswordResetRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ConfirmAdminPasswordResetCommand(request.Email, request.Code, request.NewPassword), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>
    /// Sign in or sign up with Google, Apple or Facebook in one step - returns tokens directly
    /// (the provider already authenticated the user, so there's no OTP step). Links to an existing
    /// account when the provider's verified email matches it. Internal roles can't use this.
    /// </summary>
    [HttpPost("social")]
    public async Task<IActionResult> SocialLogin(SocialLoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SocialLoginCommand(request.Provider, request.Token, request.Name, request.AccountType), cancellationToken);
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
