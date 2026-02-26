/// <summary>
/// We will use this resume as template for filtering and matching vacancies.
/// And for helping LLM to generate a Cover Letter.
/// </summary>
public class Resume
{
    public Guid Id;
    public Guid UserId;
    public string Title;
    public decimal Salary;
    public Currency Currency;
    public string[] Skills;
    public WorkLocationType WorkLocationType;
    public Experience Experience;
    public string[] Projects;
    public string[] Certifications;
    public LanguageProficiency[] Languages;
    public Education[] Educations;

    public Location[] Locations;
    public string[] ExcludedWords;
    public DateTime CreatedAt;
    public DateTime UpdatedAt;
}
