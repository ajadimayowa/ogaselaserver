using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.ConfirmPasswordReset;

/// <summary>
/// The "request" half of password reset is just the existing POST /auth/otp/request - no
/// separate endpoint needed, since generating and SMS-ing a code doesn't care why the caller
/// wants one. This is the "confirm" half: verify that code, then set NewPassword.
/// </summary>
public sealed record ConfirmPasswordResetCommand(string Phone, string Code, string NewPassword) : IRequest<Result>;
