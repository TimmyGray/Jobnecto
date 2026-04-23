using FluentAssertions;
using JobNecto.Infrastructure.Services;

namespace JobNecto.Tests.Infrastructure.Security;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_SameInput_ProducesDifferentHashes()
    {
        const string password = "Password123!";

        var firstHash = _hasher.HashPassword(password);
        var secondHash = _hasher.HashPassword(password);

        firstHash.Should().NotBe(secondHash);
    }

    [Fact]
    public void VerifyHashedPassword_WithCorrectPassword_ReturnsTrue()
    {
        const string password = "Password123!";
        var hash = _hasher.HashPassword(password);

        var isValid = _hasher.VerifyHashedPassword(hash, password);

        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyHashedPassword_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.HashPassword("Password123!");

        var isValid = _hasher.VerifyHashedPassword(hash, "WrongPassword!");

        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyHashedPassword_WithInvalidHashFormat_ReturnsFalse()
    {
        var isValid = _hasher.VerifyHashedPassword("not-a-valid-hash", "Password123!");

        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsHashSupportedFormat_WithValidAndInvalidValues_ReturnsExpectedResult()
    {
        var hash = _hasher.HashPassword("Password123!");

        _hasher.IsHashSupportedFormat(hash).Should().BeTrue();
        _hasher.IsHashSupportedFormat("plain-text-password").Should().BeFalse();
    }

    [Fact]
    public void IsHashSupportedFormat_WithTooFewIterations_ReturnsFalse()
    {
        var salt = Convert.ToBase64String(new byte[16]);
        var hash = Convert.ToBase64String(new byte[32]);
        var weakHash = $"pbkdf2-sha256$1${salt}${hash}";

        _hasher.IsHashSupportedFormat(weakHash).Should().BeFalse();
    }

    [Fact]
    public void IsHashSupportedFormat_WithUndersizedPayload_ReturnsFalse()
    {
        var undersizedSalt = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        var undersizedHash = Convert.ToBase64String(new byte[] { 4, 5, 6 });
        var malformed = $"pbkdf2-sha256$100000${undersizedSalt}${undersizedHash}";

        _hasher.IsHashSupportedFormat(malformed).Should().BeFalse();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("+100000")]
    [InlineData("100000.5")]
    [InlineData("100,000")]
    public void IsHashSupportedFormat_WithMalformedIterationToken_ReturnsFalse(string iterationToken)
    {
        var salt = Convert.ToBase64String(new byte[16]);
        var hash = Convert.ToBase64String(new byte[32]);
        var malformed = $"pbkdf2-sha256${iterationToken}${salt}${hash}";

        _hasher.IsHashSupportedFormat(malformed).Should().BeFalse();
    }
}