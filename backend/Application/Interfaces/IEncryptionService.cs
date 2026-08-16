namespace NeverMissLead.Application.Interfaces;

/// <summary>
/// Service contract for encrypting and decrypting sensitive PII fields (name, phone, email).
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts plaintext into a base64-encoded ciphertext. Returns null if plaintext is null or empty.
    /// </summary>
    string? Encrypt(string? plainText);

    /// <summary>
    /// Decrypts a base64-encoded ciphertext back to plaintext. Returns null if ciphertext is null or empty.
    /// </summary>
    string? Decrypt(string? cipherText);
}
