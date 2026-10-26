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
    /// Performs necessary enum parsing. Empty values are stored as <c>null</c>, the same
    /// representation <see cref="ApplyUpdates"/> uses for a cleared field.
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
                : Enum.TryParse<Currency>(command.Currency, true, out var parsedCurrency)
                    ? parsedCurrency
                    : null,
            Skills = command.Skills is { Length: > 0 } ? command.Skills : null,
            WorkLocationType = Enum.TryParse<WorkLocationType>(command.WorkLocationType, true, out var parsedWlt)
                ? parsedWlt
                : null,
            Experience = string.IsNullOrWhiteSpace(command.Experience)
                ? null
                : Enum.TryParse<Experience>(command.Experience, true, out var parsedExp)
                    ? parsedExp
                    : null,
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
            Currency = resume.Currency?.ToString() ?? string.Empty,
            Skills = resume.Skills ?? Array.Empty<string>(),
            WorkLocationType = resume.WorkLocationType?.ToString() ?? string.Empty,
            Experience = resume.Experience?.ToString() ?? string.Empty,
            Projects = resume.Projects,
            Certifications = resume.Certifications,
            Languages = resume.Languages,
            Locations = resume.Locations,
            ExcludedWords = resume.ExcludedWords,
            CreatedAt = resume.CreatedAt,
            UpdatedAt = resume.UpdatedAt
        };
    }

    /// <summary>
    /// Applies provided update fields to an existing <see cref="Resume"/> entity.
    /// A <c>null</c> (omitted) field is left unchanged. An empty value clears the field and is
    /// stored as <c>null</c>: <c>""</c> (or whitespace) for currency, work-location type and
    /// experience, and an empty array for skills. Salary is cleared by
    /// <see cref="UpdateResumeCommand.ClearSalary"/>.
    /// </summary>
    /// <param name="resume">Target entity to update.</param>
    /// <param name="command">Update payload.</param>
    public static void ApplyUpdates(this Resume resume, UpdateResumeCommand command)
    {
        if (command.Title != null)
        {
            resume.Title = command.Title;
        }

        if (command.ClearSalary)
        {
            resume.Salary = null;
        }
        else if (command.Salary.HasValue)
        {
            resume.Salary = command.Salary.Value;
        }

        if (command.Currency != null)
        {
            resume.Currency = string.IsNullOrWhiteSpace(command.Currency)
                ? null
                : Enum.TryParse<Currency>(command.Currency, true, out var parsedCurrency)
                    ? parsedCurrency
                    : null;
        }

        if (command.Skills != null)
        {
            resume.Skills = command.Skills.Length == 0 ? null : command.Skills;
        }

        if (command.WorkLocationType != null)
        {
            resume.WorkLocationType = string.IsNullOrWhiteSpace(command.WorkLocationType)
                ? null
                : Enum.TryParse<WorkLocationType>(command.WorkLocationType, true, out var parsedWorkLocationType)
                    ? parsedWorkLocationType
                    : null;
        }

        if (command.Experience != null)
        {
            resume.Experience = string.IsNullOrWhiteSpace(command.Experience)
                ? null
                : Enum.TryParse<Experience>(command.Experience, true, out var parsedExperience)
                    ? parsedExperience
                    : null;
        }

        if (command.Projects != null)
        {
            resume.Projects = command.Projects;
        }

        if (command.Certifications != null)
        {
            resume.Certifications = command.Certifications;
        }

        if (command.Languages != null)
        {
            resume.Languages = command.Languages;
        }

        if (command.Locations != null)
        {
            resume.Locations = command.Locations;
        }

        if (command.ExcludedWords != null)
        {
            resume.ExcludedWords = command.ExcludedWords;
        }
    }
}
