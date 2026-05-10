using JobNecto.Domain.Entities;

namespace JobNecto.Application.Vacancies.Mappers;

/// <summary>
/// Mapping extension methods for vacancy list/query DTOs.
/// </summary>
public static class VacancyMappers
{
    /// <summary>
    /// Maps a <see cref="Vacancy"/> domain entity to a list-item response DTO.
    /// </summary>
    /// <param name="vacancy">The vacancy entity to map.</param>
    /// <returns>The mapped list-item DTO.</returns>
    public static VacancyListItemResult ToVacancyListItemResult(this Vacancy vacancy)
    {
        ArgumentNullException.ThrowIfNull(vacancy);

        return new VacancyListItemResult
        {
            Id = vacancy.Id,
            Title = vacancy.Title,
            Company = vacancy.Company,
            WorkLocationType = vacancy.WorkLocationType?.ToString(),
            Location = vacancy.Location?.ToString(),
            Salary = vacancy.SalaryMin.HasValue || vacancy.SalaryMax.HasValue
                ? new VacancySalaryResult
                {
                    Min = vacancy.SalaryMin,
                    Max = vacancy.SalaryMax,
                }
                : null,
            Currency = vacancy.Currency?.ToString(),
            CreatedAt = vacancy.CreatedAt,
        };
    }

    /// <summary>
    /// Maps a <see cref="Vacancy"/> domain entity to a full detail response DTO.
    /// </summary>
    /// <param name="vacancy">The vacancy entity to map.</param>
    /// <returns>The mapped detail DTO.</returns>
    public static VacancyDetailResult ToVacancyDetailResult(this Vacancy vacancy)
    {
        ArgumentNullException.ThrowIfNull(vacancy);

        return new VacancyDetailResult
        {
            Id = vacancy.Id,
            Title = vacancy.Title,
            Description = vacancy.Description,
            Company = vacancy.Company,
            Skills = vacancy.Skills,
            WorkLocationType = vacancy.WorkLocationType?.ToString(),
            Location = vacancy.Location?.ToString(),
            Salary = vacancy.SalaryMin.HasValue || vacancy.SalaryMax.HasValue
                ? new VacancySalaryResult
                {
                    Min = vacancy.SalaryMin,
                    Max = vacancy.SalaryMax,
                }
                : null,
            Currency = vacancy.Currency?.ToString(),
            MatchScore = vacancy.MatchScore,
            JobSource = new VacancyJobSourceResult
            {
                Name = vacancy.JobSource.Name,
                Url = vacancy.JobSource.Url,
            },
            Categories = vacancy.JobCategories,
            ExperienceLevel = vacancy.ExperienceLevel,
            CreatedAt = vacancy.CreatedAt,
        };
    }
}