using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.Educations;

/// <summary>
/// Command for soft-deleting an existing education record.
/// </summary>
public class DeleteEducationCommand : IRequest<Unit>
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
}
