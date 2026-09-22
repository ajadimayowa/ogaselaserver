namespace Ogasela.Domain.Accounts;

public class RefreshToken
{
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    /// <summary>
    /// True when this token was superseded by a rotation (as opposed to being revoked
    /// directly, e.g. via logout). Presenting a rotated token again indicates reuse.
    /// </summary>
    public bool WasRotated => ReplacedByTokenId is not null;

    public static RefreshToken Create(Guid userId, string tokenHash, DateTime expiresAt)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            RevokedAt = null,
            ReplacedByTokenId = null
        };
    }

    public bool IsActive(DateTime now) => !IsRevoked && ExpiresAt > now;

    public void Revoke(DateTime revokedAt)
    {
        RevokedAt = revokedAt;
    }

    public void RotateInto(Guid replacementTokenId, DateTime revokedAt)
    {
        RevokedAt = revokedAt;
        ReplacedByTokenId = replacementTokenId;
    }
}
