using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.CoverLetterTemplates;

/// <summary>
/// Command for soft-deleting a cover letter template owned by the authenticated user.
/// </summary>
public class DeleteCoverLetterTemplateCommand : IRequest<Unit>
{
    /// <summary>
    /// Cover letter template identifier from the route.
    /// </summary>
    [JsonIgnore]
    public Guid CoverLetterTemplateId { get; set; }

    /// <summary>
    /// Authenticated user identifier from the security context.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }
}
