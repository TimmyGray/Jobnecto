using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.CoverLetters;

/// <summary>
/// Command for updating content of an existing cover letter.
/// </summary>
public class UpdateCoverLetterCommand : IRequest<CoverLetterUpdateResult>
{
    /// <summary>
    /// Cover letter identifier from route.
    /// </summary>
    [JsonIgnore]
    public Guid CoverLetterId { get; set; }

    /// <summary>
    /// Authenticated user identifier injected by API layer.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// Updated cover letter content.
    /// </summary>
    public string Content { get; set; } = null!;
}

/// <summary>
/// Response payload for an updated cover letter.
/// </summary>
public class CoverLetterUpdateResult
{
    public Guid Id { get; set; }
    public Guid VacancyId { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}