using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Resumes.Mappers;
using MediatR;

namespace JobNecto.Application.Resumes;

/// <summary>
/// Handles retrieval of a single resume by ID for the authenticated user.
/// </summary>
public class GetResumeQueryHandler : IRequestHandler<GetResumeQuery, ResumeResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetResumeQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public GetResumeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Retrieves the resume with the specified ID, verifying ownership.
    /// Throws <see cref="NotFoundException"/> if the resume does not exist,
    /// is soft-deleted, or belongs to a different user.
    /// </summary>
    /// <param name="request">The query containing ResumeId and UserId.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapped <see cref="ResumeResult"/>.</returns>
    public async Task<ResumeResult> Handle(GetResumeQuery request, CancellationToken cancellationToken)
    {
        var resume = await _unitOfWork.ResumeRepository.GetByIdAsync(request.ResumeId, cancellationToken);

        // Enforce user-scope: return 404 (not 403) to prevent existence leakage
        if (resume.UserId != request.UserId)
            throw new NotFoundException("Resume", request.ResumeId);

        return resume.ToResumeResult();
    }
}
