using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.CoverLetters;

/// <summary>
/// Command for soft-deleting an existing cover letter.
/// </summary>
public class DeleteCoverLetterCommand : IRequest<Unit>
{
    /// <summary>
    /// Cover letter identifier from route.
    /// </summary>
    [JsonIgnore]
    public Guid CoverLetterId { get; set; }

    /// <summary>
    /// Authenticated user identifier from security context.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }
}