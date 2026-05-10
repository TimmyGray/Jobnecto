using JobNecto.Domain.Enums;
using JobNecto.Domain.ValueObjects;
using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.Vacancies;

/// <summary>
/// Query to browse or filter vacancies through a single cursor-paginated endpoint.
/// Empty criteria means browse mode.
/// </summary>
public class FilterVacanciesQuery : IRequest<PagedResult<VacancyListItemResult>>
{
    /// <summary>
    /// ID of the authenticated user making the request.
    /// Set by the API controller from the security context.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// Number of items per page. Defaults to 20, capped at 100.
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// Optional sorting mode.
    /// Supported values: <c>createdAt</c> (default), <c>updatedAt</c>, and <c>relevance</c> (alias of <c>updatedAt</c>).
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Cursor: ID of the last seen vacancy from the previous page.
    /// </summary>
    public Guid? LastSeenId { get; set; }

    /// <summary>
    /// Cursor timestamp paired with <see cref="LastSeenId"/>.
    /// Holds <c>CreatedAt</c> when <see cref="SortBy"/> is <c>createdAt</c> (default), or <c>UpdatedAt</c>
    /// when <see cref="SortBy"/> is <c>updatedAt</c>/<c>relevance</c>.
    /// Must be reset to null when <see cref="SortBy"/> changes between requests; mixing cursor values
    /// from a different sort mode produces undefined pagination results.
    /// </summary>
    public DateTime? LastSeenUpdatedAt { get; set; }

    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Company { get; set; }
    public Location[]? Location { get; set; }
    public WorkTimeType[]? WorkTimeType { get; set; }
    public WorkLocationType[]? WorkLocationType { get; set; }
    public string[]? JobCategories { get; set; }
    public string[]? Skills { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public Currency[]? Currency { get; set; }
    public double? MatchScore { get; set; }
    public string[]? ExperienceLevel { get; set; }
    public string[]? JobSource { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool? IsChosen { get; set; }
}

/// <summary>
/// Vacancy list item DTO for browse/filter responses.
/// </summary>
public class VacancyListItemResult
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? WorkLocationType { get; set; }
    public string? Location { get; set; }
    public VacancySalaryResult? Salary { get; set; }
    public string? Currency { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Salary representation in vacancy list results.
/// </summary>
public class VacancySalaryResult
{
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
}