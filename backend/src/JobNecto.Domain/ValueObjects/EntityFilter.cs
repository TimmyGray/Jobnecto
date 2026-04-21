using JobNecto.Domain.Enums;

namespace JobNecto.Domain.ValueObjects;

public record VacancyFilter
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Company { get; init; }
    public Location[]? Location { get; init; }
    public WorkTimeType[]? WorkTimeType { get; init; }
    public WorkLocationType[]? WorkLocationType { get; init; }
    public string[]? JobCategories { get; init; }
    public string[]? Skills { get; init; }
    public decimal? SalaryMin { get; init; }
    public decimal? SalaryMax { get; init; }
    public Currency[]? Currency { get; init; }
    public double? MatchScore { get; init; }
    public string[]? ExperienceLevel { get; init; }
    public string[]? JobSource { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public bool? IsChosen { get; init; }
}