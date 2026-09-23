using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.VerifyLoginOtp;

/// <summary>Step 2 of consumer login: the OTP LoginCommand sent. No password here - having a code to submit at all already proves step 1 passed. Phone/Email should match whichever identifier was used for LoginCommand.</summary>
public sealed record VerifyLoginOtpCommand(string? Phone, string? Email, string Code) : IRequest<Result<AuthTokenResponse>>;
