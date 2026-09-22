using Ogasela.Application.AdIntegrations;

namespace Ogasela.UnitTests.AdIntegrations;

/// <summary>Pass-through - unit tests only need something implementing ITokenEncryptor, not real encryption.</summary>
public sealed class FakeTokenEncryptor : ITokenEncryptor
{
    public string Encrypt(string plaintext) => plaintext;

    public string Decrypt(string ciphertext) => ciphertext;
}
