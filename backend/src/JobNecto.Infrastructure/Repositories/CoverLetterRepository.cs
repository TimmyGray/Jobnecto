using JobNecto.Application.CoverLetters;
using JobNecto.Application.Interfaces;
using JobNecto.Domain.ValueObjects;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobNecto.Infrastructure.Repositories;

public class CoverLetterRepository : SoftDeletableRepository<CoverLetter>, ICoverLetterRepository
{
    private sealed class CoverLetterRow
    {
        public required CoverLetter CoverLetter { get; init; }
        public Vacancy? Vacancy { get; init; }
    }

    public CoverLetterRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<PagedResult<CoverLetterListItem>> GetPagedListAsync(
        PagedQuery pagedQuery,
        CancellationToken ct)
    {
        if (!pagedQuery.UserId.HasValue)
            throw new ArgumentException("UserId is required for cover letter listing.", nameof(pagedQuery));

        var pageSize = pagedQuery.PageSize < 1 ? 20 : Math.Min(pagedQuery.PageSize, 100);
        var userId = pagedQuery.UserId.Value;

        var baseQuery = _context
            .Set<CoverLetter>()
            .AsNoTracking()
            .Where(cl => cl.UserId == userId && !cl.IsDeleted);

        var totalCount = await baseQuery.CountAsync(ct);

        IQueryable<CoverLetterRow> query = baseQuery
            .GroupJoin(
                _context.Set<Vacancy>().AsNoTracking().IgnoreQueryFilters(),
                cl => cl.VacancyId,
                v => v.Id,
                (cl, vacancies) => new CoverLetterRow
                {
                    CoverLetter = cl,
                    Vacancy = vacancies.FirstOrDefault(),
                }
            )
            .OrderByDescending(x => x.CoverLetter.CreatedAt)
            .ThenByDescending(x => x.CoverLetter.Id);

        if (pagedQuery.LastSeenId is Guid lastSeenId && pagedQuery.LastSeenUpdatedAt is DateTime cursorCreatedAt)
        {
            query = query.Where(x =>
                x.CoverLetter.CreatedAt < cursorCreatedAt
                || (x.CoverLetter.CreatedAt == cursorCreatedAt && x.CoverLetter.Id < lastSeenId));
        }

        var pagePlusOne = await query.Take(pageSize + 1).ToListAsync(ct);
        var page = pagePlusOne.Take(pageSize).ToList();
        var hasNext = pagePlusOne.Count > pageSize;

        var items = page
            .Select(x => new CoverLetterListItem
            {
                Id = x.CoverLetter.Id,
                VacancyId = x.CoverLetter.VacancyId,
                VacancyTitle = x.Vacancy?.Title,
                CreatedAt = x.CoverLetter.CreatedAt,
                UpdatedAt = x.CoverLetter.UpdatedAt,
            })
            .ToList();

        return new PagedResult<CoverLetterListItem>(
            items,
            totalCount,
            items.Count > 0 ? items[^1].Id : null,
            // For this endpoint, LastSeenUpdatedAt carries CreatedAt for cursor semantics.
            items.Count > 0 ? items[^1].CreatedAt : null,
            pageSize,
            hasNext);
    }

    /// <inheritdoc />
    public async Task<CoverLetterDetailResult?> GetDetailByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context
            .Set<CoverLetter>()
            .AsNoTracking()
            .Where(cl => cl.Id == id && !cl.IsDeleted)
            .GroupJoin(
                _context.Set<Vacancy>().AsNoTracking().IgnoreQueryFilters(),
                cl => cl.VacancyId,
                v => v.Id,
                (cl, vacancies) => new CoverLetterRow
                {
                    CoverLetter = cl,
                    Vacancy = vacancies.FirstOrDefault(),
                }
            )
            .Select(x => new CoverLetterDetailResult
            {
                Id = x.CoverLetter.Id,
                UserId = x.CoverLetter.UserId,
                VacancyId = x.CoverLetter.VacancyId,
                Content = x.CoverLetter.Content,
                CreatedAt = x.CoverLetter.CreatedAt,
                UpdatedAt = x.CoverLetter.UpdatedAt,
                Vacancy = new VacancyInCoverLetterResult
                {
                    Id = x.Vacancy != null ? x.Vacancy.Id : x.CoverLetter.VacancyId,
                    Title = x.Vacancy != null ? x.Vacancy.Title : null,
                    Company = x.Vacancy != null ? x.Vacancy.Company : null,
                    WorkLocationType = x.Vacancy != null ? x.Vacancy.WorkLocationType : null,
                    Location = x.Vacancy != null ? x.Vacancy.Location : null,
                },
            })
            .FirstOrDefaultAsync(ct);
    }
}