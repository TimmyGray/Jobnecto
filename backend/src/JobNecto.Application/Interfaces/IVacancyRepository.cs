using JobNecto.Domain.Entities;
using JobNecto.Domain.ValueObjects;

namespace JobNecto.Application.Interfaces;

/// <summary>Vacancy-specific query and persistence contract. Extends soft delete capability.</summary>
public interface IVacancyRepository : IRepository<Vacancy>, ISoftDeleteRepository<Vacancy>
{
    public Task<PagedResult<Vacancy>> GetFilteredAsync(PagedQuery pagedQuery, VacancyFilter? filter = null, CancellationToken ct = default);
    public Task<Vacancy> UpdateMatchScoreAsync(Guid id, double matchScore, CancellationToken ct);
}