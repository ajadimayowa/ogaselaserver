using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.AdminLogin;

/// <summary>Step 1 of the admin portal's two-factor login: email + password. On success an OTP has been sent to the account's email and phone; complete sign-in with VerifyAdminLoginOtpCommand.</summary>
public sealed record AdminLoginCommand(string Email, string Password) : IRequest<Result<AdminLoginResponse>>;

public sealed record AdminLoginResponse(string MaskedEmail, string MaskedPhone);
