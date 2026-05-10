using JobNecto.Application.CoverLetters;
using JobNecto.Domain.Entities;
using JobNecto.Domain.ValueObjects;

namespace JobNecto.Application.Interfaces;

/// <summary>
/// Cover letter specific repository contract.
/// Extends mutable operations with specialized list/detail queries.
/// </summary>
public interface ICoverLetterRepository : IMutableRepository<CoverLetter>
{
    /// <summary>
    /// Returns a cursor-paginated list of cover letters for the requested user,
    /// ordered by CreatedAt descending.
    /// </summary>
    Task<PagedResult<CoverLetterListItem>> GetPagedListAsync(PagedQuery pagedQuery, CancellationToken ct);

    /// <summary>
    /// Returns detail data for a single cover letter, including related vacancy fields.
    /// Returns null when the cover letter does not exist (or is filtered out by soft-delete).
    /// </summary>
    Task<CoverLetterDetailResult?> GetDetailByIdAsync(Guid id, CancellationToken ct);
}