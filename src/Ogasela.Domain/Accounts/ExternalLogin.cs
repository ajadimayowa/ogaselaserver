namespace Ogasela.Domain.Accounts;

/// <summary>
/// Links a <see cref="User"/> to an account at a social sign-in provider. ProviderUserId is the
/// provider's stable subject id (Google/Apple "sub", Facebook user id) - never the email, which a
/// user can change at the provider. A user may have one link per provider.
/// </summary>
public class ExternalLogin
{
    private ExternalLogin()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public ExternalLoginProvider Provider { get; private set; }

    public string ProviderUserId { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    public static ExternalLogin Create(Guid userId, ExternalLoginProvider provider, string providerUserId, DateTime now)
    {
        return new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ProviderUserId = providerUserId,
            CreatedAt = now
        };
    }
}
