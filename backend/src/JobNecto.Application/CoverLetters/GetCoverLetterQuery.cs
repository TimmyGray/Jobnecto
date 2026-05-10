using JobNecto.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.CoverLetters;

/// <summary>
/// Query for retrieving a single cover letter detail for the authenticated user.
/// </summary>
public class GetCoverLetterQuery : IRequest<CoverLetterDetailResult>
{
    /// <summary>
    /// Cover letter identifier from route.
    /// </summary>
    [JsonIgnore]
    public Guid CoverLetterId { get; set; }

    /// <summary>
    /// Authenticated user identifier injected by the API layer.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }
}

/// <summary>
/// Detail response for a single cover letter.
/// </summary>
public class CoverLetterDetailResult
{
    public Guid Id { get; set; }

    [JsonIgnore]
    public Guid UserId { get; set; }

    public Guid VacancyId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public VacancyInCoverLetterResult Vacancy { get; set; } = null!;
}

/// <summary>
/// Nested vacancy fields returned with a cover letter detail response.
/// </summary>
public class VacancyInCoverLetterResult
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Company { get; set; }
    public WorkLocationType? WorkLocationType { get; set; }
    public Location? Location { get; set; }
}