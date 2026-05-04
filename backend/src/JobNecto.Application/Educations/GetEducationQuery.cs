using MediatR;

namespace JobNecto.Application.Educations;

/// <summary>
/// Query to retrieve a single education record by ID, scoped to the current user.
/// </summary>
public class GetEducationQuery : IRequest<EducationResult>
{
    /// <summary>Gets the ID of the education record to retrieve.</summary>
    public Guid EducationId { get; init; }

    /// <summary>Gets the ID of the authenticated user making the request.</summary>
    public Guid UserId { get; init; }
}
