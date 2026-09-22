using Ogasela.Shared;

namespace Ogasela.Application.Accounts;

public static class AccountErrors
{
    public static readonly Error UserAlreadyExists = new(
        "User.AlreadyExists", "A user with this phone number or email already exists.");

    public static readonly Error InvalidCredentials = new(
        "User.InvalidCredentials", "The phone/email or password is incorrect.");

    public static readonly Error UserNotFound = new(
        "User.NotFound", "The user could not be found.");

    public static readonly Error OtpInvalid = new(
        "Otp.InvalidCode", "The verification code is incorrect.");

    public static readonly Error OtpExpired = new(
        "Otp.Expired", "The verification code has expired. Request a new one.");

    public static readonly Error OtpRateLimited = new(
        "Otp.RateLimited", "Too many verification attempts. Try again later.");

    public static readonly Error MfaRequired = new(
        "User.MfaRequired", "This account requires email + OTP sign-in. Use /api/v1/auth/admin/login instead.");

    public static readonly Error MfaPhoneRequired = new(
        "User.MfaPhoneRequired", "This admin account has no phone number on file, so a login OTP cannot be sent. Contact a Super Admin to add one.");

    public static readonly Error RefreshTokenInvalid = new(
        "RefreshToken.Invalid", "The refresh token is invalid.");

    public static readonly Error RefreshTokenExpired = new(
        "RefreshToken.Expired", "The refresh token has expired. Please log in again.");

    public static readonly Error RefreshTokenReused = new(
        "RefreshToken.Reused", "This refresh token has already been used. All sessions for this account have been revoked.");

    public static readonly Error CurrentPasswordIncorrect = new(
        "User.CurrentPasswordIncorrect", "The current password is incorrect.");
}
