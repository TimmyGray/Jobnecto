using MediatR;
using JobNecto.Application.Interfaces;
using JobNecto.Application.Resumes.Mappers;
using JobNecto.Domain.ValueObjects;

namespace JobNecto.Application.Resumes;

/// <summary>
/// Handler for <see cref="ListResumesQuery"/>. Returns a cursor-paginated list of the user's resumes.
/// </summary>
public class ListResumesQueryHandler : IRequestHandler<ListResumesQuery, PagedResult<ResumeResult>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="ListResumesQueryHandler"/>.
    /// </summary>
    /// <param name="unitOfWork">Unit of work providing repository access.</param>
    public ListResumesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>
    /// Handles the list resumes query: pages through user-scoped resume records and projects entities to DTOs.
    /// </summary>
    /// <param name="request">The query parameters including user ID, page size, and cursor values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of <see cref="ResumeResult"/> DTOs.</returns>
    public async Task<PagedResult<ResumeResult>> Handle(ListResumesQuery request, CancellationToken cancellationToken)
    {
        var cappedPageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var pagedQuery = new PagedQuery
        {
            UserId = request.UserId,
            PageSize = cappedPageSize,
            LastSeenId = request.LastSeenId,
            LastSeenUpdatedAt = request.LastSeenUpdatedAt,
        };

        var result = await _unitOfWork.ResumeRepository.GetAsync(pagedQuery, cancellationToken);

        var projectedItems = result.Items.Select(r => r.ToResumeResult()).ToList();

        return new PagedResult<ResumeResult>(
            projectedItems,
            result.TotalCount,
            result.LastSeenId,
            result.LastSeenUpdatedAt,
            result.PageSize,
            result.HasNext
        );
    }
}