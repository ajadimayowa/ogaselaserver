using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.ConfirmPasswordReset;

/// <summary>
/// Step 2 of a consumer password reset: the code from password-reset/request (or, for phone,
/// the older otp/request) plus the new password. Exactly one of Phone/Email - whichever the code
/// was sent to.
/// </summary>
public sealed record ConfirmPasswordResetCommand(string? Phone, string Code, string NewPassword, string? Email = null) : IRequest<Result>;
