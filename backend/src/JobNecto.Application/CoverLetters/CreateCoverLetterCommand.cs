using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.CoverLetters;

/// <summary>
/// Command for creating a new cover letter for a specific vacancy.
/// </summary>
public class CreateCoverLetterCommand : IRequest<CreateCoverLetterResult>
{
    /// <summary>
    /// Authenticated user identifier injected by the API layer.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// Vacancy identifier for which the cover letter is created.
    /// </summary>
    public Guid VacancyId { get; set; }

    /// <summary>
    /// Cover letter content body (50-10000 characters).
    /// </summary>
    public string Content { get; set; } = null!;
}

/// <summary>
/// Response payload for a created cover letter.
/// </summary>
public class CreateCoverLetterResult
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}