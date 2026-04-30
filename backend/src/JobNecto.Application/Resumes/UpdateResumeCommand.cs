using JobNecto.Domain.Enums;
using JobNecto.Domain.ValueObjects;
using MediatR;
using System.Text.Json.Serialization;

namespace JobNecto.Application.Resumes;

/// <summary>
/// Command for updating one or more fields on an existing resume.
/// </summary>
public class UpdateResumeCommand : IRequest<ResumeResult>
{
    /// <summary>
    /// Resume identifier from route.
    /// </summary>
    [JsonIgnore]
    public Guid ResumeId { get; set; }

    /// <summary>
    /// Authenticated user identifier from security context.
    /// </summary>
    [JsonIgnore]
    public Guid UserId { get; set; }

    /// <summary>
    /// Updated resume title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Updated expected salary.
    /// </summary>
    public decimal? Salary { get; set; }

    /// <summary>
    /// Updated salary currency.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Updated skills collection.
    /// </summary>
    public string[]? Skills { get; set; }

    /// <summary>
    /// Updated preferred work location type.
    /// </summary>
    public string? WorkLocationType { get; set; }

    /// <summary>
    /// Updated experience level.
    /// </summary>
    public string? Experience { get; set; }

    /// <summary>
    /// Updated projects list.
    /// </summary>
    public string[]? Projects { get; set; }

    /// <summary>
    /// Updated certifications list.
    /// </summary>
    public string[]? Certifications { get; set; }

    /// <summary>
    /// Updated language proficiencies.
    /// </summary>
    public LanguageProficiency[]? Languages { get; set; }

    /// <summary>
    /// Updated preferred locations.
    /// </summary>
    public Location[]? Locations { get; set; }

    /// <summary>
    /// Updated excluded keywords list.
    /// </summary>
    public string[]? ExcludedWords { get; set; }
}
