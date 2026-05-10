using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;
using JobNecto.Domain.ValueObjects;


/// <summary>
/// Realistic <see cref="Vacancy"/> fixtures for repository and filter tests.
/// Use <see cref="CreateOwnerUser"/> (or your own user) and pass <paramref name="userId"/> into each factory.
/// Timestamps: pass explicit <c>createdAt</c>/<c>updatedAt</c> when testing pagination (ordering is by <c>CreatedAt</c> desc).
/// </summary>
/// <remarks>
/// <see cref="VacancyRepository"/> filters on <c>Skills</c> / <c>JobCategories</c> are not exercised with EF InMemory in this project
/// (that provider often fails to translate those LINQ shapes). Use PostgreSQL-backed tests for those filters when you need automated coverage.
/// </remarks>
internal static class VacancyTestData
{
    /// <summary>Stable “now” for narrative consistency across fixtures.</summary>
    public static readonly DateTime AnchorUtc = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    public static JobSource SourceLinkedIn { get; } =
        new() { Name = "LinkedIn", Url = "https://www.linkedin.com/jobs" };

    public static JobSource SourcePracujPl { get; } =
        new() { Name = "Pracuj.pl", Url = "https://www.pracuj.pl" };

    public static JobSource SourceNoFluffJobs { get; } =
        new() { Name = "NoFluffJobs", Url = "https://nofluffjobs.com" };

    public static JobSource SourceGreenhouse { get; } =
        new() { Name = "Greenhouse", Url = "https://boards.greenhouse.io" };

    public static User CreateOwnerUser(Guid? id = null, DateTime? createdAt = null, DateTime? updatedAt = null)
    {
        var at = createdAt ?? AnchorUtc;
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            Login = "vacancy-repo-tester",
            Password = "test",
            Email = "vacancy.tests@example.com",
            Name = "Alex",
            SecondName = "Tester",
            CreatedAt = at,
            UpdatedAt = updatedAt ?? at,
        };
    }

    /// <summary>Remote senior backend — good for skills filter (.NET, Azure), location Poland, high match.</summary>
    public static Vacancy SeniorDotNetRemotePoland(
        Guid userId,
        DateTime createdAt,
        DateTime updatedAt,
        Guid? id = null
    ) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Title = "Senior Backend Engineer (.NET / Azure)",
            Description =
                "You'll design APIs and event-driven services for our clinical data platform. "
                + "Stack: .NET 8+, PostgreSQL, Azure Service Bus, Kubernetes. On-call rotation once per 6 weeks.",
            Company = "MedStack Analytics",
            CompanyWebsite = "https://medstack.example",
            Location = Location.Poland,
            WorkTimeType = WorkTimeType.FullTime,
            WorkLocationType = WorkLocationType.Remote,
            JobCategories = ["Engineering", "Backend", "Healthcare IT"],
            Skills = [".NET", "C#", "Azure", "PostgreSQL", "Docker", "REST"],
            SalaryMin = 95_000m,
            SalaryMax = 118_000m,
            Currency = Currency.EUR,
            MatchScore = 0.91,
            ExperienceLevel = "Senior",
            JobSource = SourceLinkedIn,
            IsChosen = true,
            IsHidden = false,
        };

    /// <summary>Hybrid junior frontend — Berlin, EUR, React/TypeScript.</summary>
    public static Vacancy JuniorReactHybridBerlin(
        Guid userId,
        DateTime createdAt,
        DateTime updatedAt,
        Guid? id = null
    ) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Title = "Junior Frontend Developer (React)",
            Description =
                "Join the team building our B2B dashboard. You'll pair with seniors, write TypeScript and React 19, "
                + "and help improve accessibility (WCAG 2.2). 2 days/week in our Kreuzberg office.",
            Company = "CargoFlow GmbH",
            CompanyWebsite = "https://cargoflow.example",
            Location = Location.Germany,
            WorkTimeType = WorkTimeType.FullTime,
            WorkLocationType = WorkLocationType.Hybrid,
            JobCategories = ["Engineering", "Frontend"],
            Skills = ["React", "TypeScript", "HTML", "CSS", "Vitest"],
            SalaryMin = 48_000m,
            SalaryMax = 56_000m,
            Currency = Currency.EUR,
            MatchScore = 0.72,
            ExperienceLevel = "Junior",
            JobSource = SourceGreenhouse,
            IsChosen = false,
            IsHidden = false,
        };

    /// <summary>US remote contract SRE — USD, Kubernetes; different source for <see cref="VacancyFilter.JobSource"/> tests.</summary>
    public static Vacancy ContractDevOpsRemoteUs(
        Guid userId,
        DateTime createdAt,
        DateTime updatedAt,
        Guid? id = null
    ) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Title = "Site Reliability Engineer (contract, 12 months)",
            Description =
                "Hardening multi-region AWS estates, Terraform modules, incident response playbooks. "
                + "Must have prior on-call experience and strong Linux.",
            Company = "Northwind Cloud",
            CompanyWebsite = "https://northwind.example",
            Location = Location.UnitedStates,
            WorkTimeType = WorkTimeType.Contract,
            WorkLocationType = WorkLocationType.Remote,
            JobCategories = ["Engineering", "DevOps", "Infrastructure"],
            Skills = ["Kubernetes", "Terraform", "AWS", "Prometheus", "Go"],
            SalaryMin = 120_000m,
            SalaryMax = 155_000m,
            Currency = Currency.USD,
            MatchScore = 0.88,
            ExperienceLevel = "Senior",
            JobSource = SourceNoFluffJobs,
            IsChosen = false,
            IsHidden = false,
        };

    /// <summary>Warsaw on-site internship — PLN, low band; good for salary-range and experience filters.</summary>
    public static Vacancy DataAnalystInternshipWarsaw(
        Guid userId,
        DateTime createdAt,
        DateTime updatedAt,
        Guid? id = null
    ) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Title = "Data Analyst Intern (SQL & dashboards)",
            Description =
                "6-month paid internship. You'll support retail KPI reporting in BigQuery and Looker, "
                + "and learn dbt from the analytics engineering team.",
            Company = "RetailPulse SA",
            CompanyWebsite = "https://retailpulse.example",
            Location = Location.Poland,
            WorkTimeType = WorkTimeType.Internship,
            WorkLocationType = WorkLocationType.OnSite,
            JobCategories = ["Analytics", "Data"],
            Skills = ["SQL", "Python", "BigQuery", "Looker"],
            SalaryMin = 4_500m,
            SalaryMax = 5_800m,
            Currency = Currency.PLN,
            MatchScore = 0.45,
            ExperienceLevel = "Intern",
            JobSource = SourcePracujPl,
            IsChosen = false,
            IsHidden = false,
        };

    /// <summary>Part-time marketing — different domain for title/company/description search tests.</summary>
    public static Vacancy PartTimeContentMarketerRemote(
        Guid userId,
        DateTime createdAt,
        DateTime updatedAt,
        Guid? id = null
    ) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Title = "Content Marketer (part-time, B2B SaaS)",
            Description =
                "Own the blog calendar, interview customers for case studies, and repurpose webinars into LinkedIn posts. "
                + "About 20h/week, flexible hours.",
            Company = "SignalForge",
            CompanyWebsite = "https://signalforge.example",
            Location = Location.Netherlands,
            WorkTimeType = WorkTimeType.PartTime,
            WorkLocationType = WorkLocationType.Remote,
            JobCategories = ["Marketing", "Content"],
            Skills = ["Copywriting", "SEO", "HubSpot", "Google Analytics"],
            SalaryMin = 32_000m,
            SalaryMax = 42_000m,
            Currency = Currency.GBP,
            MatchScore = 0.33,
            ExperienceLevel = "Mid",
            JobSource = SourceLinkedIn,
            IsChosen = false,
            IsHidden = false,
        };

    /// <summary>Hidden vacancy — exercise <see cref="VacancyFilter.IsChosen"/> / listing behaviour if you expose hidden rows.</summary>
    public static Vacancy LegacyFullStackHidden(
        Guid userId,
        DateTime createdAt,
        DateTime updatedAt,
        Guid? id = null
    ) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Title = "Full Stack Engineer (PHP + Vue)",
            Description =
                "Maintenance of legacy monolith; migration to modular services planned 2027. "
                + "Not actively hiring — role kept for pipeline.",
            Company = "OldMill Software",
            Location = Location.CzechRepublic,
            WorkTimeType = WorkTimeType.FullTime,
            WorkLocationType = WorkLocationType.Hybrid,
            JobCategories = ["Engineering", "Full Stack"],
            Skills = ["PHP", "Vue.js", "MySQL", "Docker"],
            SalaryMin = 60_000m,
            SalaryMax = 75_000m,
            Currency = Currency.EUR,
            MatchScore = 0.12,
            ExperienceLevel = "Mid",
            JobSource = SourcePracujPl,
            IsChosen = false,
            IsHidden = true,
        };

    /// <summary>No job categories / skills — filters on categories or skills should not match this row.</summary>
    public static Vacancy BareMinimumScrapedListing(
        Guid userId,
        DateTime createdAt,
        DateTime updatedAt,
        Guid? id = null
    ) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            Title = "Engineering role — details on apply",
            Description = "Apply via external form. Title only import.",
            Company = "UnknownCo",
            Location = Location.Spain,
            WorkTimeType = WorkTimeType.FullTime,
            WorkLocationType = WorkLocationType.Remote,
            JobCategories = null,
            Skills = null,
            SalaryMin = null,
            SalaryMax = null,
            Currency = null,
            MatchScore = null,
            ExperienceLevel = null,
            JobSource = new JobSource { Name = "RSSFeed", Url = "https://jobs.example/feed.xml" },
            IsChosen = false,
            IsHidden = false,
        };

    /// <summary>
    /// Returns fixtures with distinct timestamps suitable for deterministic cursor pagination tests.
    /// </summary>
    public static IReadOnlyList<Vacancy> AllDistinctUpdatedAt(Guid userId)
    {
        var t0 = AnchorUtc;
        return
        [
            SeniorDotNetRemotePoland(userId, t0.AddDays(-10), t0.AddHours(3)),
            JuniorReactHybridBerlin(userId, t0.AddDays(-9), t0.AddHours(2)),
            ContractDevOpsRemoteUs(userId, t0.AddDays(-8), t0.AddHours(1)),
            DataAnalystInternshipWarsaw(userId, t0.AddDays(-7), t0),
            PartTimeContentMarketerRemote(userId, t0.AddDays(-6), t0.AddHours(-1)),
            LegacyFullStackHidden(userId, t0.AddDays(-20), t0.AddHours(-2)),
            BareMinimumScrapedListing(userId, t0.AddDays(-5), t0.AddHours(-3)),
        ];
    }
}
