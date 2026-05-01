using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.Resumes;

/// <summary>
/// Command for soft-deleting an existing resume.
/// </summary>
public class DeleteResumeCommand : IRequest<Unit>
{
    /// <summary>
    /// Resume identifier from route.
    /// </summary>
    [JsonIgnore]
    public Guid ResumeId { get; set; }

    /// <summary>
    /// Authenticated user identifier from security context.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }
}
