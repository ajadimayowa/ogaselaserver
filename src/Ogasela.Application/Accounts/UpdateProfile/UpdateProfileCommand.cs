using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.UpdateProfile;

public sealed record UpdateProfileCommand(string? Phone, string? Email, string? BusinessName) : IRequest<Result>;
