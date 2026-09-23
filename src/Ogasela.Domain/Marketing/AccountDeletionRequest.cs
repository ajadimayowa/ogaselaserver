namespace Ogasela.Domain.Marketing;

public enum AccountDeletionRequestType
{
    DeleteAccount,
    DeleteSpecificData,
}

public class AccountDeletionRequest
{
    private AccountDeletionRequest() { }

    public Guid Id { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public AccountDeletionRequestType RequestType { get; private set; }

    public string? Details { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static AccountDeletionRequest Create(
        string? email, string? phone, AccountDeletionRequestType requestType, string? details, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Email = email,
            Phone = phone,
            RequestType = requestType,
            Details = details,
            CreatedAt = now,
        };
}
