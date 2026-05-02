using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.Educations;

/// <summary>
/// Command for creating a new education record for the authenticated user.
/// </summary>
public class CreateEducationCommand : IRequest<EducationResult>
{
    /// <summary>
    /// Authenticated user identifier injected by the API layer.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// Degree title, for example "Bachelor of Science".
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Field of study.
    /// </summary>
    public string Specialization { get; set; } = null!;

    /// <summary>
    /// Degree level represented as text enum value.
    /// Supported values: bachelor, master, phd, postdoc, other.
    /// </summary>
    public string Degree { get; set; } = null!;
}

/// <summary>
/// Response payload for a created education record.
/// </summary>
public class EducationResult
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Specialization { get; set; } = null!;
    public string Degree { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}