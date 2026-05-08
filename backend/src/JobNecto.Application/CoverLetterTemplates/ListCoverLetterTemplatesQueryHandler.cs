using JobNecto.Application.CoverLetterTemplates.Mappers;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.ValueObjects;
using MediatR;

namespace JobNecto.Application.CoverLetterTemplates;

/// <summary>
/// Handler for <see cref="ListCoverLetterTemplatesQuery"/>.
/// Returns a cursor-paginated, optionally-searched list of the user's cover letter templates.
/// </summary>
public class ListCoverLetterTemplatesQueryHandler
    : IRequestHandler<ListCoverLetterTemplatesQuery, PagedResult<CoverLetterTemplateListItemResult>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="ListCoverLetterTemplatesQueryHandler"/>.
    /// </summary>
    /// <param name="unitOfWork">Unit of work providing repository access.</param>
    public ListCoverLetterTemplatesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>
    /// Handles the list cover letter templates query: pages through user-scoped records,
    /// applies optional name search, and projects entities to list-item DTOs.
    /// </summary>
    /// <param name="request">The query parameters including user ID, page size, cursor values, and optional search.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A paginated result of <see cref="CoverLetterTemplateListItemResult"/> DTOs.</returns>
    public async Task<PagedResult<CoverLetterTemplateListItemResult>> Handle(
        ListCoverLetterTemplatesQuery request, CancellationToken cancellationToken)
    {
        var cappedPageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var pagedQuery = new PagedQuery
        {
            UserId = request.UserId,
            PageSize = cappedPageSize,
            LastSeenId = request.LastSeenId,
            LastSeenUpdatedAt = request.LastSeenUpdatedAt,
            Search = request.Search,
        };

        var result = await _unitOfWork.CoverLetterTemplateRepository.GetAsync(pagedQuery, cancellationToken);

        var projectedItems = result.Items.Select(t => t.ToCoverLetterTemplateListItemResult()).ToList();

        return new PagedResult<CoverLetterTemplateListItemResult>(
            projectedItems,
            result.TotalCount,
            result.LastSeenId,
            result.LastSeenUpdatedAt,
            result.PageSize,
            result.HasNext
        );
    }
}
