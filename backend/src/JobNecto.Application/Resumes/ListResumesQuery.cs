using JobNecto.Application.Interfaces;
using JobNecto.Application.Resumes.Mappers;
using JobNecto.Domain.ValueObjects;
using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.Resumes;

/// <summary>
/// Query to retrieve a paginated list of resumes for the current user.
/// </summary>
public class ListResumesQuery : IRequest<PagedResult<ResumeResult>>
{
    /// <summary>
    /// ID of the authenticated user making the request.
    /// Set by the API controller from the current security context.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// Number of items per page. Defaults to 20, capped at 100.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Cursor: ID of the last seen resume. Used to advance the page window.
    /// </summary>
    public Guid? LastSeenId { get; set; }

    /// <summary>
    /// Cursor: UpdatedAt timestamp of the last seen resume. Used together with LastSeenId.
    /// </summary>
    public DateTime? LastSeenUpdatedAt { get; set; }
}

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
