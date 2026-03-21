/// <summary>
/// We will use this resume as template for filtering and matching vacancies.
/// And for helping LLM to generate a Cover Letter.
/// </summary>
public sealed class Resume : BaseEntity
{
    public Guid UserId;
    public string? Title;
    public decimal? Salary;
    public Currency? Currency;
    public string[]? Skills;
    public WorkLocationType? WorkLocationType;
    public Experience? Experience;
    public string[]? Projects;
    public string[]? Certifications;
    public LanguageProficiency[]? Languages;
    public ICollection<Education> Educations;

    public Location[]? Locations;
    public string[]? ExcludedWords;
}
