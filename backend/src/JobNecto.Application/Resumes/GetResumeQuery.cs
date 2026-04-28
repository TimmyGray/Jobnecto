using MediatR;

namespace JobNecto.Application.Resumes;

/// <summary>
/// Query to retrieve a single resume by ID, scoped to the current user.
/// </summary>
public class GetResumeQuery : IRequest<ResumeResult>
{
    /// <summary>Gets the ID of the resume to retrieve.</summary>
    public Guid ResumeId { get; init; }

    /// <summary>Gets the ID of the authenticated user making the request.</summary>
    public Guid UserId { get; init; }
}
