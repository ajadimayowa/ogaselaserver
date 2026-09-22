using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest<Result>;
