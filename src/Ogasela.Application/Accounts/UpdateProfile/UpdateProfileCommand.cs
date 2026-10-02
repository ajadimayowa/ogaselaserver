using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.UpdateProfile;

/// <summary>
/// Updates the caller's display name. Phone, email and business info are deliberately not here:
/// those changes need staff approval and go through ProfileChanges instead.
/// </summary>
public sealed record UpdateProfileCommand(string Name) : IRequest<Result>;
