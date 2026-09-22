using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.RequestOtp;

public sealed record RequestOtpCommand(string Phone) : IRequest<Result>;
