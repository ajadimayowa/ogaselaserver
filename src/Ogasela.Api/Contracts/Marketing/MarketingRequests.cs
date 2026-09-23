using Ogasela.Domain.Marketing;

namespace Ogasela.Api.Contracts.Marketing;

public sealed record SubmitContactFormRequest(
    string Name, string Email, string Subject, string Message, string TurnstileToken);

public sealed record SubmitTesterSignupRequest(string Name, string Email, string TurnstileToken);

public sealed record RequestAccountDeletionRequest(
    string? Email,
    string? Phone,
    AccountDeletionRequestType RequestType,
    string? Details,
    string TurnstileToken);
