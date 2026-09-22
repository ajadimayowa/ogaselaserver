using Microsoft.AspNetCore.DataProtection;
using Ogasela.Application.AdIntegrations;

namespace Ogasela.Infrastructure.AdIntegrations;

/// <summary>
/// Encrypts AdAccountConnection tokens at rest using ASP.NET Core's Data Protection API rather
/// than a separate secrets-manager service: Data Protection is purpose-built for exactly this
/// (symmetric encryption of app data, with automatic key generation/rotation), needs no new
/// external credentials, and - since its keys are persisted to the Redis instance this app
/// already depends on (see AddAdIntegrations) instead of local disk - stays safe to decrypt
/// from any API instance behind the load balancer.
/// </summary>
public sealed class DataProtectionTokenEncryptor : ITokenEncryptor
{
    private const string Purpose = "Ogasela.AdIntegrations.Tokens";

    private readonly IDataProtector _protector;

    public DataProtectionTokenEncryptor(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string Encrypt(string plaintext) => _protector.Protect(plaintext);

    public string Decrypt(string ciphertext) => _protector.Unprotect(ciphertext);
}
