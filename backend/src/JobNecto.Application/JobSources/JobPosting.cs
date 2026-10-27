using JobNecto.Domain.Enums;

namespace JobNecto.Application.JobSources;

/// <summary>
/// A job posting as delivered by an <see cref="IJobSource"/>, before it becomes a user's vacancy.
/// Adapters map their external format to this record and never build persistence entities themselves.
/// </summary>
public sealed record JobPosting
{
    /// <summary>Job title, e.g. "Senior .NET Engineer".</summary>
    public required string Title { get; init; }

    /// <summary>Full posting text: role, team, stack, responsibilities and requirements.</summary>
    public required string Description { get; init; }

    /// <summary>Hiring company name.</summary>
    public required string Company { get; init; }

    /// <summary>Company website, when the source provides one.</summary>
    public string? CompanyWebsite { get; init; }

    /// <summary>Country of the role, when known.</summary>
    public Location? Location { get; init; }

    /// <summary>Employment type (full-time, contract, ...), when known.</summary>
    public WorkTimeType? WorkTimeType { get; init; }

    /// <summary>On-site, remote or hybrid, when known.</summary>
    public WorkLocationType? WorkLocationType { get; init; }

    /// <summary>Free-form job categories, e.g. "Backend". Empty when the source has none.</summary>
    public IReadOnlyList<string> JobCategories { get; init; } = [];

    /// <summary>Skills the posting asks for, e.g. "C#". Empty when the source has none.</summary>
    public IReadOnlyList<string> Skills { get; init; } = [];

    /// <summary>Lower salary bound, when published.</summary>
    public decimal? SalaryMin { get; init; }

    /// <summary>Upper salary bound, when published.</summary>
    public decimal? SalaryMax { get; init; }

    /// <summary>Currency of the salary bounds, when published.</summary>
    public Currency? Currency { get; init; }

    /// <summary>Seniority label, e.g. "Junior", "Middle", "Senior" or "Lead".</summary>
    public string? ExperienceLevel { get; init; }

    /// <summary>Display name of the source the posting came from.</summary>
    public required string SourceName { get; init; }

    /// <summary>Canonical posting URL. Ingestion uses it as the per-user de-duplication key.</summary>
    public required string SourceUrl { get; init; }
}
