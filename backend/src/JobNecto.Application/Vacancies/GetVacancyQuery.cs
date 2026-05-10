using MediatR;

namespace JobNecto.Application.Vacancies;

/// <summary>
/// Query to retrieve the full detail of a single vacancy by ID, scoped to the current user.
/// </summary>
public class GetVacancyQuery : IRequest<VacancyDetailResult>
{
    /// <summary>Gets or sets the ID of the vacancy to retrieve.</summary>
    public Guid VacancyId { get; init; }

    /// <summary>Gets or sets the ID of the authenticated user making the request.</summary>
    public Guid UserId { get; init; }
}

/// <summary>
/// Full detail DTO for a single vacancy response.
/// </summary>
public class VacancyDetailResult
{
    /// <summary>Gets or sets the unique vacancy identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the vacancy title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the vacancy description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the hiring company name.</summary>
    public string? Company { get; set; }

    /// <summary>Gets or sets the required skills.</summary>
    public string[]? Skills { get; set; }

    /// <summary>Gets or sets the work location type (e.g., Remote, Hybrid, OnSite).</summary>
    public string? WorkLocationType { get; set; }

    /// <summary>Gets or sets the geographic location.</summary>
    public string? Location { get; set; }

    /// <summary>Gets or sets the salary range, or <c>null</c> if not specified.</summary>
    public VacancySalaryResult? Salary { get; set; }

    /// <summary>Gets or sets the salary currency.</summary>
    public string? Currency { get; set; }

    /// <summary>Gets or sets the computed match score, or <c>null</c> if not yet calculated.</summary>
    public double? MatchScore { get; set; }

    /// <summary>Gets or sets the source where the vacancy was found.</summary>
    public VacancyJobSourceResult? JobSource { get; set; }

    /// <summary>Gets or sets the job categories.</summary>
    public string[]? Categories { get; set; }

    /// <summary>Gets or sets the required experience level.</summary>
    public string? ExperienceLevel { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the vacancy was created.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents the source platform where a vacancy originated.
/// </summary>
public class VacancyJobSourceResult
{
    /// <summary>Gets or sets the name of the job source platform.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL of the job source.</summary>
    public string? Url { get; set; }
}
