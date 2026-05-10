using JobNecto.Domain.ValueObjects;
using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.CoverLetters;

/// <summary>
/// Query to retrieve a cursor-paginated list of cover letters for the current user.
/// </summary>
public class ListCoverLettersQuery : IRequest<PagedResult<CoverLetterListItem>>
{
    /// <summary>
    /// ID of the authenticated user making the request.
    /// Injected by the API controller.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// Number of items per page. Defaults to 20, capped at 100.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Cursor: ID of the last seen cover letter from the previous page.
    /// </summary>
    public Guid? LastSeenId { get; set; }

    /// <summary>
    /// Cursor timestamp field.
    /// For this endpoint it carries the CreatedAt value of the last seen cover letter.
    /// </summary>
    public DateTime? LastSeenUpdatedAt { get; set; }
}

/// <summary>
/// List-item response DTO for cover letter list responses.
/// </summary>
public class CoverLetterListItem
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public string? VacancyTitle { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}