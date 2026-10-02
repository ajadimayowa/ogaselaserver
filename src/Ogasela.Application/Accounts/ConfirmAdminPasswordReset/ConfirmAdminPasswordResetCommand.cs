using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.ConfirmAdminPasswordReset;

/// <summary>Step 2 of the admin portal's "forgot password" flow: the code RequestAdminPasswordResetCommand sent, plus the new password.</summary>
public sealed record ConfirmAdminPasswordResetCommand(string Email, string Code, string NewPassword) : IRequest<Result>;
