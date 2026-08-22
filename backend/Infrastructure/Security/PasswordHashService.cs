using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using NeverMissLead.Application.Interfaces;

namespace NeverMissLead.Infrastructure.Security;

/// <summary>
/// Implements PBKDF2 password hashing using ASP.NET Core's <see cref="KeyDerivation.Pbkdf2"/>.
/// Standard parameters: HMAC-SHA512, 100,000 iterations, 128-bit cryptographically secure salt,
/// and constant-time comparison via <see cref="CryptographicOperations.FixedTimeEquals"/> to prevent timing attacks.
/// </summary>
public sealed class PasswordHashService : IPasswordHashService
{
    private const int IterationCount = 100_000;
    private const int SaltSize = 16; // 128 bits
    private const int NumBytesRequested = 32; // 256 bits

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] subkey = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA512,
            iterationCount: IterationCount,
            numBytesRequested: NumBytesRequested);

        // Format: {iterations}.{saltBase64}.{subkeyBase64}
        return $"{IterationCount}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";
    }

    public bool VerifyPassword(string password, string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        try
        {
            var parts = passwordHash.Split('.');
            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], out var iterations))
                return false;

            var salt = Convert.FromBase64String(parts[1]);
            var expectedSubkey = Convert.FromBase64String(parts[2]);

            var actualSubkey = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: iterations,
                numBytesRequested: expectedSubkey.Length);

            return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
        }
        catch
        {
            return false;
        }
    }
}
