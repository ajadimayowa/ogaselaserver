using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Marketing.SubmitTesterSignup;

public sealed record SubmitTesterSignupCommand(string Name, string Email, string TurnstileToken) : IRequest<Result>;
