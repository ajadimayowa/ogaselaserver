namespace Ogasela.Domain.AdIntegrations;

/// <summary>
/// A seller's link to their own Facebook or TikTok ads account. AccessToken/RefreshToken are
/// stored encrypted at rest (see ITokenEncryptor) - this entity only ever holds ciphertext.
/// </summary>
public class AdAccountConnection
{
    private AdAccountConnection()
    {
    }

    public Guid Id { get; private set; }

    public Guid SellerId { get; private set; }

    public AdPlatform Platform { get; private set; }

    public string EncryptedAccessToken { get; private set; } = string.Empty;

    public string? EncryptedRefreshToken { get; private set; }

    public string ExternalAccountId { get; private set; } = string.Empty;

    public DateTime ConnectedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public bool IsActive => RevokedAt is null;

    public static AdAccountConnection Create(
        Guid id,
        Guid sellerId,
        AdPlatform platform,
        string encryptedAccessToken,
        string? encryptedRefreshToken,
        string externalAccountId,
        DateTime connectedAt)
    {
        return new AdAccountConnection
        {
            Id = id,
            SellerId = sellerId,
            Platform = platform,
            EncryptedAccessToken = encryptedAccessToken,
            EncryptedRefreshToken = encryptedRefreshToken,
            ExternalAccountId = externalAccountId,
            ConnectedAt = connectedAt
        };
    }

    public void Revoke(DateTime now)
    {
        RevokedAt = now;
    }
}
