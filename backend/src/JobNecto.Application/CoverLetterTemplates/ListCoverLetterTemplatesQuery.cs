using JobNecto.Domain.ValueObjects;
using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.CoverLetterTemplates;

/// <summary>
/// Query to retrieve a cursor-paginated, searchable list of cover letter templates for the current user.
/// </summary>
public class ListCoverLetterTemplatesQuery : IRequest<PagedResult<CoverLetterTemplateListItemResult>>
{
    /// <summary>
    /// ID of the authenticated user making the request.
    /// Injected by the API controller — not bound from the HTTP query string.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// Number of items per page. Defaults to 20, capped at 100.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Cursor: ID of the last seen template. Used to advance the page window.
    /// </summary>
    public Guid? LastSeenId { get; set; }

    /// <summary>
    /// Cursor: UpdatedAt timestamp of the last seen template. Used together with LastSeenId.
    /// </summary>
    public DateTime? LastSeenUpdatedAt { get; set; }

    /// <summary>
    /// Optional case-insensitive name filter. Returns only templates whose name contains this value.
    /// </summary>
    public string? Search { get; set; }
}

/// <summary>
/// List-item response DTO for a cover letter template. Returns a content preview instead of full content.
/// </summary>
public class CoverLetterTemplateListItemResult
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>
    /// First 200 characters of the template content.
    /// </summary>
    public string ContentPreview { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
