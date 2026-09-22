namespace Ogasela.Application.AdIntegrations;

/// <summary>Encrypts OAuth tokens at rest before they're stored on AdAccountConnection. See DataProtectionTokenEncryptor for the concrete implementation and technology choice.</summary>
public interface ITokenEncryptor
{
    string Encrypt(string plaintext);

    string Decrypt(string ciphertext);
}
