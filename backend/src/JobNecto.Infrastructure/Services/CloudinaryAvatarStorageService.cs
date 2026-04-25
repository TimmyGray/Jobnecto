using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using JobNecto.Application.Interfaces;
using JobNecto.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace JobNecto.Infrastructure.Services;

/// <summary>
/// Cloudinary-backed avatar media storage service.
/// </summary>
public sealed class CloudinaryAvatarStorageService : IAvatarStorageService
{
    private readonly Cloudinary? _cloudinary;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudinaryAvatarStorageService"/> class.
    /// </summary>
    public CloudinaryAvatarStorageService(IOptions<CloudinarySettings> settings)
    {
        var account = TryBuildAccount(settings.Value);
        if (account == null)
        {
            return;
        }

        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    /// <inheritdoc />
    public async Task<AvatarUploadResult> UploadUserAvatarAsync(
        Guid userId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        EnsureConfigured();

        var publicId = BuildAvatarPublicId(userId);

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            PublicId = publicId,
            Overwrite = true,
            Invalidate = true,
            UseFilename = false,
            UniqueFilename = false,
            Format = ResolveImageFormat(contentType)
        };

        var uploadResult = await _cloudinary!.UploadAsync(uploadParams);
        ct.ThrowIfCancellationRequested();

        if (uploadResult.Error != null)
        {
            throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");
        }

        var secureUrl = uploadResult.SecureUrl?.AbsoluteUri;
        if (string.IsNullOrWhiteSpace(secureUrl))
        {
            throw new InvalidOperationException("Cloudinary upload did not return a secure URL.");
        }

        return new AvatarUploadResult
        {
            SecureUrl = secureUrl,
            PublicId = publicId
        };
    }

    /// <inheritdoc />
    public async Task DeleteUserAvatarAsync(Guid userId, CancellationToken ct)
    {
        EnsureConfigured();

        var deletionParams = new DeletionParams(BuildAvatarPublicId(userId))
        {
            ResourceType = ResourceType.Image,
            Type = "upload",
            Invalidate = true
        };

        var deletionResult = await _cloudinary!.DestroyAsync(deletionParams);
        ct.ThrowIfCancellationRequested();

        if (deletionResult.Error != null)
        {
            throw new InvalidOperationException($"Cloudinary delete failed: {deletionResult.Error.Message}");
        }
    }

    private static Account? TryBuildAccount(CloudinarySettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.CloudinaryUrl))
        {
            var parsedFromUrl = TryParseCloudinaryUrl(settings.CloudinaryUrl);
            if (parsedFromUrl != null)
            {
                return parsedFromUrl;
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.CloudName)
            && !string.IsNullOrWhiteSpace(settings.ApiKey)
            && !string.IsNullOrWhiteSpace(settings.ApiSecret))
        {
            return new Account(
                settings.CloudName,
                settings.ApiKey,
                settings.ApiSecret);
        }

        return null;
    }

    private static Account? TryParseCloudinaryUrl(string cloudinaryUrl)
    {
        if (!Uri.TryCreate(cloudinaryUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!string.Equals(uri.Scheme, "cloudinary", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2)
        {
            return null;
        }

        var apiKey = Uri.UnescapeDataString(userInfo[0]);
        var apiSecret = Uri.UnescapeDataString(userInfo[1]);
        var cloudName = uri.Host;

        if (string.IsNullOrWhiteSpace(apiKey)
            || string.IsNullOrWhiteSpace(apiSecret)
            || string.IsNullOrWhiteSpace(cloudName))
        {
            return null;
        }

        return new Account(cloudName, apiKey, apiSecret);
    }

    private static string BuildAvatarPublicId(Guid userId)
    {
        return $"users/{userId:N}/avatar";
    }

    private static string ResolveImageFormat(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return "jpg";
        }

        return contentType.Trim().ToLowerInvariant() switch
        {
            "image/jpeg" => "jpg",
            "image/jpg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ => "jpg"
        };
    }

    private void EnsureConfigured()
    {
        if (_cloudinary == null)
        {
            throw new InvalidOperationException(
                "Cloudinary settings are missing.");
        }
    }
}
