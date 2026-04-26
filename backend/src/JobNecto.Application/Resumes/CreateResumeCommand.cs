using JobNecto.Domain.Enums;
using JobNecto.Domain.ValueObjects;
using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.Resumes;

/// <summary>
/// Command to create a new resume for a job seeker.
/// Contains basic information, professional skills, and work preferences.
/// </summary>
public class CreateResumeCommand : IRequest<ResumeResult>
{
    /// <summary>
    /// ID of the user who owns the resume. 
    /// Set by the API controller from the current security context.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// A descriptive title for the resume (e.g., "Senior .NET Developer").
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// List of professional skills or keywords.
    /// </summary>
    public string[] Skills { get; set; } = null!;

    /// <summary>
    /// Preferred work location type (remote, office, hybrid).
    /// </summary>
    public string WorkLocationType { get; set; } = null!;

    /// <summary>
    /// Desired salary.
    /// </summary>
    public decimal? Salary { get; set; }

    /// <summary>
    /// Currency for the desired salary.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Professional experience level.
    /// </summary>
    public string? Experience { get; set; }

    /// <summary>
    /// Notable professional projects.
    /// </summary>
    public string[]? Projects { get; set; }

    /// <summary>
    /// Professional certifications.
    /// </summary>
    public string[]? Certifications { get; set; }

    /// <summary>
    /// Language proficiencies.
    /// </summary>
    public LanguageProficiency[]? Languages { get; set; }

    /// <summary>
    /// Desired job locations.
    /// </summary>
    public Location[]? Locations { get; set; }

    /// <summary>
    /// Words or phrases to exclude from job matching.
    /// </summary>
    public string[]? ExcludedWords { get; set; }
}

/// <summary>
/// Result returned after successful resume creation.
/// </summary>
public class ResumeResult
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Title { get; set; }
    public decimal? Salary { get; set; }
    public string? Currency { get; set; }
    public string[]? Skills { get; set; }
    public string? WorkLocationType { get; set; }
    public string? Experience { get; set; }
    public string[]? Projects { get; set; }
    public string[]? Certifications { get; set; }
    public LanguageProficiency[]? Languages { get; set; }
    public Location[]? Locations { get; set; }
    public string[]? ExcludedWords { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
