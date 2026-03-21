public interface IVacancyRepository : IRepository<Vacancy>
{
    public Task<PagedResult<Vacancy>> GetFilteredAsync(PagedQuery pagedQuery, VacancyFilter? filter = null, CancellationToken ct = default);
    public Task<Vacancy> UpdateMatchScoreAsync(Guid id, double matchScore, CancellationToken ct);
}