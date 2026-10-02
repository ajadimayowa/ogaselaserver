using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.RequestAdminPasswordReset;

/// <summary>
/// Step 1 of the admin portal's "forgot password" flow: email only. Admins sign in by email and
/// the portal never knows their phone number, so the consumer phone-keyed reset
/// (otp/request + password-reset/confirm) doesn't fit. Always succeeds - see the handler.
/// </summary>
public sealed record RequestAdminPasswordResetCommand(string Email) : IRequest<Result>;
