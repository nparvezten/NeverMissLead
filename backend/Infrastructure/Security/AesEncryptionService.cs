using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Infrastructure.Security;

/// <summary>
/// Implements AES-256-CBC field encryption for PII data at rest.
/// Prepend the 16-byte initialization vector (IV) to the encrypted ciphertext.
/// </summary>
public sealed class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    // 32-byte default key used for local/dev if not provided in config
    private const string DefaultDevKey = "0123456789abcdef0123456789abcdef";

    public AesEncryptionService(IConfiguration configuration)
    {
        var configuredKey = configuration["Security:EncryptionKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            _key = Encoding.UTF8.GetBytes(DefaultDevKey);
        }
        else if (configuredKey.Length == 32)
        {
            _key = Encoding.UTF8.GetBytes(configuredKey);
        }
        else
        {
            // If base64 encoded
            try
            {
                var bytes = Convert.FromBase64String(configuredKey);
                _key = bytes.Length == 32 ? bytes : SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
            }
            catch
            {
                _key = SHA256.HashData(Encoding.UTF8.GetBytes(configuredKey));
            }
        }
    }

    public string? Encrypt(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
            return null;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to ciphertext
        var combined = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, combined, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(combined);
    }

    public string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrWhiteSpace(cipherText))
            return null;

        try
        {
            var combined = Convert.FromBase64String(cipherText);
            if (combined.Length < 16)
                return null;

            using var aes = Aes.Create();
            aes.Key = _key;

            var iv = new byte[16];
            var cipherBytes = new byte[combined.Length - 16];
            Buffer.BlockCopy(combined, 0, iv, 0, 16);
            Buffer.BlockCopy(combined, 16, cipherBytes, 0, cipherBytes.Length);

            using var decryptor = aes.CreateDecryptor(aes.Key, iv);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            return null;
        }
    }
}
