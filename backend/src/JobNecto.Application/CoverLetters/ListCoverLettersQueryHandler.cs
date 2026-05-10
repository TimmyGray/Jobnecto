using JobNecto.Application.Interfaces;
using JobNecto.Domain.ValueObjects;
using MediatR;

namespace JobNecto.Application.CoverLetters;

/// <summary>
/// Handles retrieval of cursor-paginated cover letter list items for the current user.
/// </summary>
public class ListCoverLettersQueryHandler : IRequestHandler<ListCoverLettersQuery, PagedResult<CoverLetterListItem>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListCoverLettersQueryHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Unit of work abstraction.</param>
    public ListCoverLettersQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<PagedResult<CoverLetterListItem>> Handle(
        ListCoverLettersQuery request,
        CancellationToken cancellationToken)
    {
        var cappedPageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var pagedQuery = new PagedQuery
        {
            UserId = request.UserId,
            PageSize = cappedPageSize,
            LastSeenId = request.LastSeenId,
            LastSeenUpdatedAt = request.LastSeenUpdatedAt,
        };

        return await _unitOfWork.CoverLetterRepository.GetPagedListAsync(pagedQuery, cancellationToken);
    }
}