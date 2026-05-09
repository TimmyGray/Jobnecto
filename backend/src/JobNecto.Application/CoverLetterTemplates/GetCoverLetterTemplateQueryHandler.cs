using JobNecto.Application.CoverLetterTemplates.Mappers;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using MediatR;

namespace JobNecto.Application.CoverLetterTemplates;

/// <summary>
/// Handles retrieval of a single cover letter template by ID for the authenticated user.
/// </summary>
public class GetCoverLetterTemplateQueryHandler : IRequestHandler<GetCoverLetterTemplateQuery, CoverLetterTemplateResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCoverLetterTemplateQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public GetCoverLetterTemplateQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Retrieves the cover letter template with the specified ID, verifying ownership.
    /// Throws <see cref="NotFoundException"/> if the template does not exist,
    /// is soft-deleted, or belongs to a different user.
    /// </summary>
    /// <param name="request">The query containing CoverLetterTemplateId and UserId.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapped <see cref="CoverLetterTemplateResult"/>.</returns>
    public async Task<CoverLetterTemplateResult> Handle(GetCoverLetterTemplateQuery request, CancellationToken cancellationToken)
    {
        var template = await _unitOfWork.CoverLetterTemplateRepository.GetByIdAsync(
            request.CoverLetterTemplateId, cancellationToken);

        // Enforce user-scope: return 404 (not 403) to prevent existence leakage
        if (template.UserId != request.UserId)
            throw new NotFoundException("CoverLetterTemplate", request.CoverLetterTemplateId);

        return template.ToCoverLetterTemplateResult();
    }
}
