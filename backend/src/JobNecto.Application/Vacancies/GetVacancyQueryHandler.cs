using JobNecto.Application.Exceptions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Vacancies.Mappers;
using MediatR;

namespace JobNecto.Application.Vacancies;

/// <summary>
/// Handles retrieval of a single vacancy by ID for the authenticated user.
/// </summary>
public class GetVacancyQueryHandler : IRequestHandler<GetVacancyQuery, VacancyDetailResult>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetVacancyQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public GetVacancyQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Retrieves the vacancy with the specified ID, verifying ownership.
    /// Throws <see cref="NotFoundException"/> if the vacancy does not exist,
    /// is soft-deleted, or belongs to a different user.
    /// </summary>
    /// <param name="request">The query containing VacancyId and UserId.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The mapped <see cref="VacancyDetailResult"/>.</returns>
    public async Task<VacancyDetailResult> Handle(GetVacancyQuery request, CancellationToken cancellationToken)
    {
        var vacancy = await _unitOfWork.VacancyRepository.GetByIdAsync(request.VacancyId, cancellationToken);

        if (vacancy.UserId != request.UserId)
            throw new NotFoundException("Vacancy", request.VacancyId);

        return vacancy.ToVacancyDetailResult();
    }
}
