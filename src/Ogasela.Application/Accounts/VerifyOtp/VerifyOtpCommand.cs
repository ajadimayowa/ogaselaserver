using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.VerifyOtp;

public sealed record VerifyOtpCommand(string Phone, string Code) : IRequest<Result>;
