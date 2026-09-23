using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.Login;

/// <summary>Step 1 of every consumer login: phone-or-email + password. On success an OTP has been sent to the account's phone (and email, if it has one) - complete sign-in with VerifyLoginOtpCommand. Internal roles (Moderator/FinanceAdmin/SuperAdmin/Staff) must use AdminLoginCommand instead - this always fails with User.MfaRequired for them.</summary>
public sealed record LoginCommand(string? Phone, string? Email, string Password) : IRequest<Result<LoginOtpSentResponse>>;

public sealed record LoginOtpSentResponse(string MaskedEmail, string MaskedPhone);
