using JobNecto.Domain.Entities;
using JobNecto.Domain.ValueObjects;
using JobNecto.Infrastructure.Persistance;

namespace JobNecto.Infrastructure.Repositories;

public class CoverLetterTemplateRepository : SoftDeletableRepository<CoverLetterTemplate>
{
    public CoverLetterTemplateRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Applies case-insensitive name search when <see cref="PagedQuery.Search"/> is provided.
    /// </summary>
    protected override IQueryable<CoverLetterTemplate> ApplyAdditionalFilters(
        IQueryable<CoverLetterTemplate> query, PagedQuery pagedQuery)
    {
        if (!string.IsNullOrWhiteSpace(pagedQuery.Search))
            query = query.Where(t => t.Name.ToLower().Contains(pagedQuery.Search.ToLower()));

        return query;
    }
}