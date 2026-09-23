using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Marketing.SubmitContactForm;

public sealed record SubmitContactFormCommand(
    string Name, string Email, string Subject, string Message, string TurnstileToken) : IRequest<Result>;
