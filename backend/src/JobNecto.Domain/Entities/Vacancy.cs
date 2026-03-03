public class Vacancy
{
    public Guid Id;
    public Guid UserId;
    public string? Title;
    public string? Description;
    public string? Company;

    public string? CompanyWebsite;

    public Location Location;

    public WorkTimeType WorkTimeType;

    public WorkLocationType WorkLocationType;

    public string[]? JobCategories;

    public string[]? Skills;

    public decimal SalaryMin;

    public decimal SalaryMax;

    public Currency Currency;

    public double MatchScore;

    public string? ExperienceLevel;

    public required JobSource JobSource;
    public DateTime CreatedAt;

    public DateTime UpdatedAt;

    public bool IsChosen;

    public bool IsHidden;
}
