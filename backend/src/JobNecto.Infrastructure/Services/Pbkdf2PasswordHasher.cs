using System.Security.Cryptography;
using System.Globalization;
using JobNecto.Application.Interfaces;

namespace JobNecto.Infrastructure.Services;

/// <summary>
/// PBKDF2-based password hasher that uses per-password random salt and SHA-256.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string FormatMarker = "pbkdf2-sha256";
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int Iterations = 100_000;
    private const int MaxSupportedIterations = 1_000_000;

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        }

        var salt = new byte[SaltSizeBytes];
        RandomNumberGenerator.Fill(salt);

        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSizeBytes);

        return string.Join('$',
            FormatMarker,
            Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    /// <inheritdoc />
    public bool VerifyHashedPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(providedPassword))
        {
            return false;
        }

        if (!TryParseHash(hashedPassword, out var iterations, out var salt, out var expectedHash))
        {
            return false;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            providedPassword,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    /// <inheritdoc />
    public bool IsHashSupportedFormat(string value)
    {
        return TryParseHash(value, out _, out _, out _);
    }

    private static bool TryParseHash(
        string hashedPassword,
        out int iterations,
        out byte[] salt,
        out byte[] expectedHash)
    {
        iterations = 0;
        salt = Array.Empty<byte>();
        expectedHash = Array.Empty<byte>();

        var parts = hashedPassword.Split('$');
        if (parts.Length != 4 || !string.Equals(parts[0], FormatMarker, StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations)
            || iterations < Iterations
            || iterations > MaxSupportedIterations)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
            return salt.Length == SaltSizeBytes && expectedHash.Length == HashSizeBytes;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}