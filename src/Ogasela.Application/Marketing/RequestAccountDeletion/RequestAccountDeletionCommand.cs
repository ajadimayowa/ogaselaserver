using MediatR;
using Ogasela.Domain.Marketing;
using Ogasela.Shared;

namespace Ogasela.Application.Marketing.RequestAccountDeletion;

public sealed record RequestAccountDeletionCommand(
    string? Email,
    string? Phone,
    AccountDeletionRequestType RequestType,
    string? Details,
    string TurnstileToken) : IRequest<Result>;
