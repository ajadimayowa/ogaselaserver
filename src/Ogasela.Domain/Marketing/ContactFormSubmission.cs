namespace Ogasela.Domain.Marketing;

public class ContactFormSubmission
{
    private ContactFormSubmission() { }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public static ContactFormSubmission Create(string name, string email, string subject, string message, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Subject = subject,
            Message = message,
            CreatedAt = now,
        };
}
