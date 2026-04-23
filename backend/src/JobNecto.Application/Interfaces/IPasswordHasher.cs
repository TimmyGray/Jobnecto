namespace JobNecto.Application.Interfaces;

/// <summary>
/// Provides one-way password hashing and verification primitives for authentication flows.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Generates a salted one-way hash for the provided password.
    /// </summary>
    /// <param name="password">Plaintext password from user input.</param>
    /// <returns>Persistable password hash.</returns>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plaintext password against a stored password hash.
    /// </summary>
    /// <param name="hashedPassword">Persisted hash.</param>
    /// <param name="providedPassword">Plaintext password provided by a client.</param>
    /// <returns><c>true</c> when the provided password matches the hash; otherwise <c>false</c>.</returns>
    bool VerifyHashedPassword(string hashedPassword, string providedPassword);

    /// <summary>
    /// Checks whether a persisted value is a supported hash format for this hasher.
    /// </summary>
    /// <param name="value">Persisted password value.</param>
    /// <returns><c>true</c> when the value is in this hasher's expected hash format.</returns>
    bool IsHashSupportedFormat(string value);
}