using JobNecto.Domain.Enums;
using JobNecto.Domain.ValueObjects;

namespace JobNecto.Domain.Entities;

public sealed class Vacancy : SoftDeletableEntity
{
    public Guid UserId;
    public string? Title;
    public string? Description;
    public string? Company;

    public string? CompanyWebsite;

    public Location? Location;

    public WorkTimeType? WorkTimeType;

    public WorkLocationType? WorkLocationType;

    public string[]? JobCategories;

    public string[]? Skills;

    public decimal? SalaryMin;

    public decimal? SalaryMax;

    public Currency? Currency;

    public double? MatchScore;

    public string? ExperienceLevel;

    public required JobSource JobSource;

    public bool IsViewed;

    public bool IsChosen;

    public bool IsHidden;
}
