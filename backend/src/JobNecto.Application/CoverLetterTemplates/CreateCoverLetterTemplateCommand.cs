using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.CoverLetterTemplates;

/// <summary>
/// Command for creating a new cover letter template for the authenticated user.
/// </summary>
public class CreateCoverLetterTemplateCommand : IRequest<CoverLetterTemplateResult>
{
    /// <summary>
    /// Authenticated user identifier injected by the API layer.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// Template name, must be unique per user across non-deleted templates.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Cover letter content body (50–10,000 characters).
    /// </summary>
    public string Content { get; set; } = null!;
}

/// <summary>
/// Response payload for a created or retrieved cover letter template.
/// </summary>
public class CoverLetterTemplateResult
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
