using MediatR;

namespace JobNecto.Application.CoverLetterTemplates;

/// <summary>
/// Query to retrieve a single cover letter template by ID, scoped to the current user.
/// </summary>
public class GetCoverLetterTemplateQuery : IRequest<CoverLetterTemplateResult>
{
    /// <summary>Gets the ID of the cover letter template to retrieve.</summary>
    public Guid CoverLetterTemplateId { get; init; }

    /// <summary>Gets the ID of the authenticated user making the request.</summary>
    public Guid UserId { get; init; }
}
