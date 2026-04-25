using FluentValidation;

namespace JobNecto.Application.Users.Validators;

/// <summary>
/// FluentValidation rules for current-user avatar upload command.
/// </summary>
public class UploadCurrentUserAvatarCommandValidator : AbstractValidator<UploadCurrentUserAvatarCommand>
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/gif"
    ];

    public UploadCurrentUserAvatarCommandValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(IsAllowedContentType)
            .WithMessage("contentType must be one of: image/jpeg, image/jpg, image/png, image/webp, image/gif.");

        RuleFor(x => x.Content)
            .NotNull()
            .Must(content => content.Length > 0)
            .WithMessage("avatar file content is required.")
            .Must(content => content.Length <= 5 * 1024 * 1024)
            .WithMessage("avatar file size must be less than or equal to 5 MB.");

        RuleFor(x => x)
            .Must(HasMatchingContentSignature)
            .When(x => x.Content is { Length: > 0 } && !string.IsNullOrWhiteSpace(x.ContentType))
            .WithMessage("avatar file signature does not match contentType.");
    }

    /// <summary>
    /// Checks whether the supplied content type is one of the allowed image MIME types.
    /// </summary>
    private static bool IsAllowedContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return AllowedContentTypes.Contains(contentType.Trim().ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that file bytes match the declared MIME type.
    /// </summary>
    private static bool HasMatchingContentSignature(UploadCurrentUserAvatarCommand command)
    {
        if (command.Content == null || command.Content.Length == 0 || string.IsNullOrWhiteSpace(command.ContentType))
        {
            return false;
        }

        var contentType = command.ContentType.Trim().ToLowerInvariant();

        return contentType switch
        {
            "image/jpeg" or "image/jpg" => IsJpeg(command.Content),
            "image/png" => IsPng(command.Content),
            "image/webp" => IsWebp(command.Content),
            "image/gif" => IsGif(command.Content),
            _ => false
        };
    }

    /// <summary>
    /// Checks JPEG magic bytes.
    /// </summary>
    private static bool IsJpeg(byte[] content)
    {
        return content.Length >= 3
            && content[0] == 0xFF
            && content[1] == 0xD8
            && content[2] == 0xFF;
    }

    /// <summary>
    /// Checks PNG magic bytes.
    /// </summary>
    private static bool IsPng(byte[] content)
    {
        return content.Length >= 8
            && content[0] == 0x89
            && content[1] == 0x50
            && content[2] == 0x4E
            && content[3] == 0x47
            && content[4] == 0x0D
            && content[5] == 0x0A
            && content[6] == 0x1A
            && content[7] == 0x0A;
    }

    /// <summary>
    /// Checks WebP magic bytes.
    /// </summary>
    private static bool IsWebp(byte[] content)
    {
        return content.Length >= 12
            && content[0] == 0x52
            && content[1] == 0x49
            && content[2] == 0x46
            && content[3] == 0x46
            && content[8] == 0x57
            && content[9] == 0x45
            && content[10] == 0x42
            && content[11] == 0x50;
    }

    /// <summary>
    /// Checks GIF magic bytes.
    /// </summary>
    private static bool IsGif(byte[] content)
    {
        return content.Length >= 6
            && content[0] == 0x47
            && content[1] == 0x49
            && content[2] == 0x46
            && content[3] == 0x38
            && (content[4] == 0x37 || content[4] == 0x39)
            && content[5] == 0x61;
    }
}
