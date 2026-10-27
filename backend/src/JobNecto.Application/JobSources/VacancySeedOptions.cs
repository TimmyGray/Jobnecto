namespace JobNecto.Application.JobSources;

/// <summary>
/// Binds <c>Seed:Vacancies</c>. Gates both seeding triggers: ingestion on registration and the
/// startup backfill for existing users.
/// </summary>
public sealed class VacancySeedOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Seed:Vacancies";

    /// <summary>When <c>false</c> (the default), no vacancies are seeded.</summary>
    public bool Enabled { get; set; }
}
