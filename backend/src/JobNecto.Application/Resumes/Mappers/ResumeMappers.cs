using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;

namespace JobNecto.Application.Resumes.Mappers;

/// <summary>
/// Mapping extension methods for Resume entity and related DTOs.
/// Decouples Domain entities from Application patterns.
/// </summary>
public static class ResumeMappers
{
    /// <summary>
    /// Maps a CreateResumeCommand to a Resume domain entity.
    /// Performs necessary enum parsing.
    /// </summary>
    public static Resume ToEntity(this CreateResumeCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        return new Resume
        {
            UserId = command.UserId,
            Title = command.Title,
            Salary = command.Salary,
            Currency = string.IsNullOrWhiteSpace(command.Currency) 
                ? null 
                : Enum.Parse<Currency>(command.Currency, true),
            Skills = command.Skills,
            WorkLocationType = Enum.Parse<WorkLocationType>(command.WorkLocationType, true),
            Experience = string.IsNullOrWhiteSpace(command.Experience) 
                ? null 
                : Enum.Parse<Experience>(command.Experience, true),
            Projects = command.Projects,
            Certifications = command.Certifications,
            Languages = command.Languages,
            Locations = command.Locations,
            ExcludedWords = command.ExcludedWords
        };
    }

    /// <summary>
    /// Maps a Resume domain entity to a ResumeResult DTO.
    /// </summary>
    public static ResumeResult ToResumeResult(this Resume resume)
    {
        if (resume == null)
            throw new ArgumentNullException(nameof(resume));

        return new ResumeResult
        {
            Id = resume.Id,
            UserId = resume.UserId,
            Title = resume.Title ?? string.Empty,
            Salary = resume.Salary,
            Currency = resume.Currency?.ToString().ToLower() ?? string.Empty,
            Skills = resume.Skills ?? Array.Empty<string>(),
            WorkLocationType = resume.WorkLocationType.ToString().ToLower(),
            Experience = resume.Experience?.ToString().ToLower() ?? string.Empty,
            Projects = resume.Projects,
            Certifications = resume.Certifications,
            Languages = resume.Languages,
            Locations = resume.Locations,
            ExcludedWords = resume.ExcludedWords,
            CreatedAt = resume.CreatedAt,
            UpdatedAt = resume.UpdatedAt
        };
    }
}
