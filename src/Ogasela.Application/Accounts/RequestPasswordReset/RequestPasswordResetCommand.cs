using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.RequestPasswordReset;

public enum PasswordResetChannel
{
    Email,
    Phone
}

/// <summary>
/// Step 1 of a consumer password reset: the user picks where the 6-digit code goes. Email needs
/// Email, Phone needs Phone. Always succeeds - see the handler. Complete with ConfirmPasswordResetCommand
/// using the same email or phone.
/// </summary>
public sealed record RequestPasswordResetCommand(PasswordResetChannel Channel, string? Email, string? Phone) : IRequest<Result>;
