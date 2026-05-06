using Microsoft.EntityFrameworkCore;
using JobNecto.Application.Interfaces;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Domain.Entities;
using JobNecto.Domain.ValueObjects;

namespace JobNecto.Infrastructure.Repositories;

public class VacancyRepository : BaseRepository<Vacancy>, IVacancyRepository, ISoftDeleteRepository<Vacancy>
{
    public VacancyRepository(AppDbContext context)
        : base(context) { }

    public async Task<PagedResult<Vacancy>> GetFilteredAsync(
        PagedQuery pagedQuery,
        VacancyFilter? filter = null,
        CancellationToken ct = default
    )
    {
        var query = _dbSet.AsNoTracking();
        query = ApplyFilters(query, filter);

        var totalCount = await query.CountAsync(ct);

        query = query
            .OrderByDescending(v => v.UpdatedAt)
            .ThenByDescending(v => v.Id)
            .ThenByDescending(v => v.MatchScore ?? 0);

        if (pagedQuery.LastSeenId is not null)
        {
            query = query.Where(v =>
                v.UpdatedAt < pagedQuery.LastSeenUpdatedAt
                || (v.UpdatedAt == pagedQuery.LastSeenUpdatedAt && v.Id < pagedQuery.LastSeenId)
            );
        }

        var take = Math.Max(1, pagedQuery.PageSize);

        var takePagePlusOne = await query.Take(take + 1).ToListAsync(ct);

        var items = takePagePlusOne.Take(take).ToList();

        var hasNext = takePagePlusOne.Count > take;

        Guid? nextLastSeenId = items.Count > 0 ? items[^1].Id : null;
        DateTime? nextLastSeenUpdatedAt = items.Count > 0 ? items[^1].UpdatedAt : null;

        return new PagedResult<Vacancy>(
            items,
            totalCount,
            nextLastSeenId,
            nextLastSeenUpdatedAt,
            pagedQuery.PageSize,
            hasNext
        );
    }

    private IQueryable<Vacancy> ApplyFilters(
        IQueryable<Vacancy> query,
        VacancyFilter? filter = null
    )
    {
        if (filter == null)
        {
            return query;
        }

        if (!string.IsNullOrWhiteSpace(filter.Title))
        {
            var term = $"%{filter.Title.Trim()}%";
            query = query.Where(v => EF.Functions.Like(v.Title, term));
        }

        if (!string.IsNullOrWhiteSpace(filter.Description))
        {
            var term = $"%{filter.Description.Trim()}%";
            query = query.Where(v => EF.Functions.Like(v.Description, term));
        }

        if (!string.IsNullOrWhiteSpace(filter.Company))
        {
            var term = $"%{filter.Company.Trim()}%";
            query = query.Where(v => EF.Functions.Like(v.Company, term));
        }

        if (filter.Location != null && filter.Location.Length > 0)
        {
            query = query.Where(v =>
                v.Location.HasValue && filter.Location.Contains(v.Location.Value)
            );
        }

        if (filter.WorkTimeType != null && filter.WorkTimeType.Length > 0)
        {
            query = query.Where(v =>
                v.WorkTimeType.HasValue && filter.WorkTimeType.Contains(v.WorkTimeType.Value)
            );
        }

        if (filter.WorkLocationType != null && filter.WorkLocationType.Length > 0)
        {
            query = query.Where(v =>
                v.WorkLocationType.HasValue
                && filter.WorkLocationType.Contains(v.WorkLocationType.Value)
            );
        }

        if (filter.JobCategories != null && filter.JobCategories.Length > 0)
        {
            query = query.Where(v =>
                v.JobCategories != null
                && filter.JobCategories.Any(c => v.JobCategories.Contains(c))
            );
        }

        if (filter.Skills != null && filter.Skills.Length > 0)
        {
            query = query.Where(v =>
                v.Skills != null && filter.Skills.Any(s => v.Skills.Contains(s))
            );
        }

        if (filter.SalaryMin != null)
        {
            query = query.Where(v => v.SalaryMin >= filter.SalaryMin);
        }

        if (filter.SalaryMax != null)
        {
            query = query.Where(v => v.SalaryMax <= filter.SalaryMax);
        }

        if (filter.Currency != null && filter.Currency.Length > 0)
        {
            query = query.Where(v =>
                v.Currency.HasValue && filter.Currency.Contains(v.Currency.Value)
            );
        }

        if (filter.MatchScore != null)
        {
            query = query.Where(v => v.MatchScore >= filter.MatchScore);
        }

        if (filter.ExperienceLevel != null && filter.ExperienceLevel.Length > 0)
        {
            query = query.Where(v =>
                v.ExperienceLevel != null && filter.ExperienceLevel.Contains(v.ExperienceLevel)
            );
        }

        if (filter.JobSource != null && filter.JobSource.Length > 0)
        {
            query = query.Where(v => filter.JobSource.Contains(v.JobSource.Name));
        }

        if (filter.IsChosen != null)
        {
            query = query.Where(v => v.IsChosen == filter.IsChosen);
        }

        if (filter.CreatedAt != null)
        {
            query = query.Where(v => v.CreatedAt >= filter.CreatedAt);
        }

        if (filter.UpdatedAt != null)
        {
            query = query.Where(v => v.UpdatedAt >= filter.UpdatedAt);
        }

        return query;
    }

    public Task<Vacancy> UpdateMatchScoreAsync(Guid id, double matchScore, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Marks <paramref name="entity"/> as soft-deleted and stages the update for the next
    /// <c>SaveChangesAsync</c> call. 
    /// </summary>
    public Task SoftDeleteAsync(Vacancy entity, CancellationToken ct)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }
}
