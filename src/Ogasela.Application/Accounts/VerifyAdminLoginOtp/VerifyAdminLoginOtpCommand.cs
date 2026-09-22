using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.VerifyAdminLoginOtp;

/// <summary>Step 2 of the admin portal's two-factor login: the OTP AdminLoginCommand sent. No password here - passing step 1 (and therefore having a code to submit at all) already proved it.</summary>
public sealed record VerifyAdminLoginOtpCommand(string Email, string Code) : IRequest<Result<AuthTokenResponse>>;
