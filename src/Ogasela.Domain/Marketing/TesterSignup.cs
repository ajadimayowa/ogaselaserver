namespace Ogasela.Domain.Marketing;

public class TesterSignup
{
    private TesterSignup() { }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public static TesterSignup Create(string name, string email, DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            CreatedAt = now,
        };
}
