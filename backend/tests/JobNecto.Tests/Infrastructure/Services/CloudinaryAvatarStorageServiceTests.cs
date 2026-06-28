using FluentAssertions;
using JobNecto.Infrastructure.Configuration;
using JobNecto.Infrastructure.Services;
using Microsoft.Extensions.Options;

namespace JobNecto.Tests.Infrastructure.Services;

public class CloudinaryAvatarStorageServiceTests
{
    private static CloudinaryAvatarStorageService Create(CloudinarySettings settings) =>
        new(Options.Create(settings));

    [Fact]
    public void Constructor_WithCloudinaryUrl_ConfiguresWithoutThrowing()
    {
        var act = () => Create(new CloudinarySettings { CloudinaryUrl = "cloudinary://mykey:mysecret@mycloud" });

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithDiscreteCredentials_ConfiguresWithoutThrowing()
    {
        var act = () => Create(new CloudinarySettings { CloudName = "c", ApiKey = "k", ApiSecret = "s" });

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("http://example.com")]          // wrong scheme
    [InlineData("::: not a uri")]               // unparseable
    [InlineData("cloudinary://onlyuser@host")]  // userinfo has no secret
    [InlineData("cloudinary://:@host")]         // empty key and secret
    public async Task Constructor_WithUnusableUrlAndNoCredentials_LeavesServiceUnconfigured(string url)
    {
        var service = Create(new CloudinarySettings { CloudinaryUrl = url });

        var act = async () => await service.DeleteUserAvatarAsync(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cloudinary settings are missing*");
    }

    [Fact]
    public async Task UploadUserAvatarAsync_WhenUnconfigured_ThrowsInvalidOperation()
    {
        var service = Create(new CloudinarySettings());

        using var content = new MemoryStream([1, 2, 3]);
        var act = async () => await service.UploadUserAvatarAsync(
            Guid.NewGuid(), content, "a.png", "image/png", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cloudinary settings are missing*");
    }

    [Fact]
    public void BuildAvatarPublicId_UsesUserScopedPath()
    {
        var userId = Guid.NewGuid();

        CloudinaryAvatarStorageService.BuildAvatarPublicId(userId)
            .Should().Be($"users/{userId:N}/avatar");
    }

    [Theory]
    [InlineData("image/jpeg", "jpg")]
    [InlineData("image/jpg", "jpg")]
    [InlineData("IMAGE/PNG", "png")]
    [InlineData("image/webp", "webp")]
    [InlineData("image/gif", "gif")]
    [InlineData("application/octet-stream", "jpg")]
    [InlineData(null, "jpg")]
    [InlineData("   ", "jpg")]
    public void ResolveImageFormat_MapsContentTypeToExtension(string? contentType, string expected)
    {
        CloudinaryAvatarStorageService.ResolveImageFormat(contentType).Should().Be(expected);
    }
}
