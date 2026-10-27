using JobNecto.Domain.Entities;
using JobNecto.Domain.ValueObjects;

namespace JobNecto.Application.Interfaces;

/// <summary>Vacancy-specific query and persistence contract. Extends soft delete capability.</summary>
public interface IVacancyRepository : ISoftDeleteRepository<Vacancy>
{
    public Task<PagedResult<Vacancy>> GetFilteredAsync(PagedQuery pagedQuery, VacancyFilter? filter = null, CancellationToken ct = default);
    public Task<Vacancy> UpdateMatchScoreAsync(Guid id, double matchScore, CancellationToken ct);

    /// <summary>
    /// Returns the job-source URLs of every vacancy the user owns, soft-deleted ones included,
    /// so ingestion never re-adds a vacancy the user removed.
    /// </summary>
    /// <param name="userId">Owner whose vacancies are inspected.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The distinct non-null URLs; empty when the user has none.</returns>
    public Task<IReadOnlyCollection<string>> GetJobSourceUrlsAsync(Guid userId, CancellationToken ct);
}