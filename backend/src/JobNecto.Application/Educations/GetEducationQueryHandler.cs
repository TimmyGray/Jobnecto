using JobNecto.Application.Educations.Mappers;
using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using MediatR;

namespace JobNecto.Application.Educations;

/// <summary>
/// Handles retrieval of a single education record by ID for the authenticated user.
/// </summary>
public class GetEducationQueryHandler : IRequestHandler<GetEducationQuery, EducationResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetEducationQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public GetEducationQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Retrieves the education record with the specified ID, verifying ownership.
    /// Throws <see cref="NotFoundException"/> if the record does not exist,
    /// is soft-deleted, or belongs to a different user.
    /// </summary>
    /// <param name="request">The query containing EducationId and UserId.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapped <see cref="EducationResult"/>.</returns>
    public async Task<EducationResult> Handle(GetEducationQuery request, CancellationToken cancellationToken)
    {
        var education = await _unitOfWork.EducationRepository.GetByIdAsync(request.EducationId, cancellationToken);

        // Enforce user-scope: return 404 (not 403) to prevent existence leakage
        if (education.UserId != request.UserId)
            throw new NotFoundException("Education", request.EducationId);

        return education.ToEducationResult();
    }
}
