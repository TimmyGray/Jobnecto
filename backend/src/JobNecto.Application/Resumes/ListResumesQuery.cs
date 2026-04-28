using JobNecto.Domain.ValueObjects;
using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.Resumes;

/// <summary>
/// Query to retrieve a paginated list of resumes for the current user.
/// </summary>
public class ListResumesQuery : IRequest<PagedResult<ResumeResult>>
{
    /// <summary>
    /// ID of the authenticated user making the request.
    /// Set by the API controller from the current security context.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// Number of items per page. Defaults to 20, capped at 100.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Cursor: ID of the last seen resume. Used to advance the page window.
    /// </summary>
    public Guid? LastSeenId { get; set; }

    /// <summary>
    /// Cursor: UpdatedAt timestamp of the last seen resume. Used together with LastSeenId.
    /// </summary>
    public DateTime? LastSeenUpdatedAt { get; set; }
}
