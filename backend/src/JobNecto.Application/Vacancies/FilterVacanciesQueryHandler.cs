using JobNecto.Application.Interfaces;
using JobNecto.Application.Vacancies.Mappers;
using JobNecto.Domain.ValueObjects;
using MediatR;

namespace JobNecto.Application.Vacancies;

/// <summary>
/// Handles vacancy browse/filter queries through a single endpoint contract.
/// </summary>
public class FilterVacanciesQueryHandler
    : IRequestHandler<FilterVacanciesQuery, PagedResult<VacancyListItemResult>>
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of <see cref="FilterVacanciesQueryHandler"/>.
    /// </summary>
    /// <param name="unitOfWork">Unit of work that provides vacancy repository access.</param>
    public FilterVacanciesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    /// <summary>
    /// Executes vacancy browse/filter query with cursor pagination.
    /// </summary>
    /// <param name="request">The filter query payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of vacancy items matching the optional criteria.</returns>
    public async Task<PagedResult<VacancyListItemResult>> Handle(
        FilterVacanciesQuery request,
        CancellationToken cancellationToken
    )
    {
        var cappedPageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

        var pagedQuery = new PagedQuery
        {
            UserId = request.UserId,
            PageSize = cappedPageSize,
            SortBy = request.SortBy,
            LastSeenId = request.LastSeenId,
            LastSeenUpdatedAt = request.LastSeenUpdatedAt,
        };

        var filter = BuildFilterOrNull(request);

        var result = await _unitOfWork.VacancyRepository.GetFilteredAsync(
            pagedQuery,
            filter,
            cancellationToken
        );

        var projectedItems = result.Items.Select(v => v.ToVacancyListItemResult()).ToList();

        return new PagedResult<VacancyListItemResult>(
            projectedItems,
            result.TotalCount,
            result.LastSeenId,
            result.LastSeenUpdatedAt,
            result.PageSize,
            result.HasNext
        );
    }

    private static VacancyFilter? BuildFilterOrNull(FilterVacanciesQuery request)
    {
        var filter = new VacancyFilter
        {
            Title = NormalizeText(request.Title),
            Description = NormalizeText(request.Description),
            Company = NormalizeText(request.Company),
            Location = NormalizeArray(request.Location),
            WorkTimeType = NormalizeArray(request.WorkTimeType),
            WorkLocationType = NormalizeArray(request.WorkLocationType),
            JobCategories = NormalizeArray(request.JobCategories),
            Skills = NormalizeArray(request.Skills),
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            Currency = NormalizeArray(request.Currency),
            MatchScore = request.MatchScore,
            ExperienceLevel = NormalizeArray(request.ExperienceLevel),
            JobSource = NormalizeArray(request.JobSource),
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            IsChosen = request.IsChosen,
        };

        return HasAnyFilterCriteria(filter) ? filter : null;
    }

    private static string? NormalizeText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static T[]? NormalizeArray<T>(T[]? values) => values is { Length: > 0 } ? values : null;

    private static bool HasAnyFilterCriteria(VacancyFilter filter) =>
        filter.Title is not null
        || filter.Description is not null
        || filter.Company is not null
        || filter.Location is not null
        || filter.WorkTimeType is not null
        || filter.WorkLocationType is not null
        || filter.JobCategories is not null
        || filter.Skills is not null
        || filter.SalaryMin.HasValue
        || filter.SalaryMax.HasValue
        || filter.Currency is not null
        || filter.MatchScore.HasValue
        || filter.ExperienceLevel is not null
        || filter.JobSource is not null
        || filter.CreatedAt.HasValue
        || filter.UpdatedAt.HasValue
        || filter.IsChosen.HasValue;
}