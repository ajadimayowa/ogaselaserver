namespace Ogasela.Application.Accounts;

/// <summary>
/// IOtpService keys codes by a plain string. Phone resets keep using the bare phone number (the
/// same key otp/request and the login OTP use); email resets get their own namespace so an email
/// address can never collide with a phone key.
/// </summary>
public static class PasswordResetOtpKey
{
    public static string ForEmail(string email) => $"email:{email.Trim().ToLowerInvariant()}";

    public static string ForPhone(string phone) => phone.Trim();
}
