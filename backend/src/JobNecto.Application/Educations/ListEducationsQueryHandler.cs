using JobNecto.Application.Educations.Mappers;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.ValueObjects;
using MediatR;

namespace JobNecto.Application.Educations;

/// <summary>
/// Handler for <see cref="ListEducationsQuery"/>. Returns a cursor-paginated list of the user's education records.
/// </summary>
public class ListEducationsQueryHandler
    : IRequestHandler<ListEducationsQuery, PagedResult<EducationResult>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="ListEducationsQueryHandler"/>.
    /// </summary>
    /// <param name="unitOfWork">Unit of work providing repository access.</param>
    public ListEducationsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>
    /// Handles the list educations query: pages through user-scoped education records and projects entities to DTOs.
    /// </summary>
    /// <param name="request">The query parameters including user ID, page size, and cursor values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of <see cref="EducationResult"/> DTOs.</returns>
    public async Task<PagedResult<EducationResult>> Handle(
        ListEducationsQuery request,
        CancellationToken cancellationToken
    )
    {
        var cappedPageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var pagedQuery = new PagedQuery
        {
            UserId = request.UserId,
            PageSize = cappedPageSize,
            LastSeenId = request.LastSeenId,
            LastSeenUpdatedAt = request.LastSeenUpdatedAt,
        };

        var result = await _unitOfWork.EducationRepository.GetAsync(pagedQuery, cancellationToken);

        var projectedItems = result.Items.Select(e => e.ToEducationResult()).ToList();

        return new PagedResult<EducationResult>(
            projectedItems,
            result.TotalCount,
            result.LastSeenId,
            result.LastSeenUpdatedAt,
            result.PageSize,
            result.HasNext
        );
    }
}
