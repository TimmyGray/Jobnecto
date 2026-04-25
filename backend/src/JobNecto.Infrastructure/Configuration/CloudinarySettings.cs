namespace JobNecto.Infrastructure.Configuration;

/// <summary>
/// Cloudinary credentials and connection settings.
/// </summary>
public sealed class CloudinarySettings
{
    /// <summary>
    /// Optional Cloudinary URL in the form cloudinary://&lt;api_key&gt;:&lt;api_secret&gt;@&lt;cloud_name&gt;.
    /// </summary>
    public string? CloudinaryUrl { get; set; }

    /// <summary>
    /// Cloudinary cloud name.
    /// </summary>
    public string? CloudName { get; set; }

    /// <summary>
    /// Cloudinary API key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Cloudinary API secret.
    /// </summary>
    public string? ApiSecret { get; set; }
}
