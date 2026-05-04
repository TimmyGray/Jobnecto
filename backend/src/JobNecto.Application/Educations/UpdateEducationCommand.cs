using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.Educations;

/// <summary>
/// Command for updating one or more fields on an existing education record.
/// </summary>
public class UpdateEducationCommand : IRequest<EducationResult>
{
    /// <summary>
    /// Education record identifier from route.
    /// </summary>
    [JsonIgnore]
    public Guid EducationId { get; set; }

    /// <summary>
    /// Authenticated user identifier from security context.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// Updated degree title. Null means no change.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Updated field of study. Null means no change.
    /// </summary>
    public string? Specialization { get; set; }

    /// <summary>
    /// Updated degree level. Supported values: bachelor, master, phd, postdoc, other. Null means no change.
    /// </summary>
    public string? Degree { get; set; }
}
