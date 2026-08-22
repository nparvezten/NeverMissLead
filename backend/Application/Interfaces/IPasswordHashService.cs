namespace NeverMissLead.Application.Interfaces;

/// <summary>
/// Service for cryptographically hashing and verifying owner passwords using PBKDF2 with HMAC-SHA512 and per-user salt.
/// </summary>
public interface IPasswordHashService
{
    /// <summary>
    /// Hashes a plaintext password with a random cryptographic salt.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies that a plaintext password matches a stored PBKDF2 password hash.
    /// </summary>
    bool VerifyPassword(string password, string? passwordHash);
}
