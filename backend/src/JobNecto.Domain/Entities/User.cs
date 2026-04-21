using JobNecto.Domain.Enums;
using JobNecto.Domain.ValueObjects;

namespace JobNecto.Domain.Entities;

public sealed class User : SoftDeletableEntity
{
    public string? Name;
    public string? SecondName;
    public int? Age;
    public required string Login;
    public required string Password;
    public required string Email;
    public string? Phone;
    public Location? Location;
    public string? Avatar;
    public string[]? Skills;
    public ICollection<Education>? Educations;
    public LanguageProficiency[]? Languages;
    public string? AboutMe;
    public string[]? Certificates;
    public string[]? Projects;
    public ICollection<Resume>? Resumes;
    public ICollection<CoverLetter>? CoverLetters;
    public ICollection<CoverLetterTemplate>? CoverLetterTemplates;
}
