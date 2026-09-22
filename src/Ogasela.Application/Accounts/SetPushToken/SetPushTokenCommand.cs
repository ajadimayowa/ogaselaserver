using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.SetPushToken;

/// <summary>Token is null to explicitly clear it (e.g. on logout from a device), non-null to (re)register it.</summary>
public sealed record SetPushTokenCommand(string? Token) : IRequest<Result>;
