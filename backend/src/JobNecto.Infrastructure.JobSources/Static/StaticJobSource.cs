using System.Text.Json;
using System.Text.Json.Serialization;
using JobNecto.Application.JobSources;
using JobNecto.Domain.Enums;

namespace JobNecto.Infrastructure.JobSources.Static;

/// <summary>
/// <see cref="IJobSource"/> backed by the synthetic demo catalogue embedded in this assembly.
/// The catalogue is parsed once per instance; register it as a singleton.
/// </summary>
public sealed class StaticJobSource : IJobSource
{
    /// <summary>Display name stored on every seeded vacancy's job source.</summary>
    public const string SourceName = "Jobnecto Demo Catalogue";

    /// <summary>Base URL of every catalogue posting. The reserved <c>.test</c> TLD never resolves to a real site.</summary>
    public const string BaseUrl = "https://jobs.example.test/demo/";

    internal const string ResourceName = "JobNecto.Infrastructure.JobSources.SeedData.demo-vacancies.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        RespectNullableAnnotations = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    private readonly Lazy<IReadOnlyList<JobPosting>> _postings;

    /// <summary>
    /// Initializes a new instance of the <see cref="StaticJobSource"/> class over the embedded catalogue.
    /// </summary>
    public StaticJobSource()
        : this(() => typeof(StaticJobSource).Assembly.GetManifestResourceStream(ResourceName))
    {
    }

    /// <summary>Test seam: reads the catalogue from <paramref name="openCatalogue"/> instead of the embedded resource.</summary>
    internal StaticJobSource(Func<Stream?> openCatalogue)
    {
        _postings = new Lazy<IReadOnlyList<JobPosting>>(() => Load(openCatalogue));
    }

    /// <inheritdoc />
    public string Name => SourceName;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The catalogue is missing or malformed.</exception>
    public Task<IReadOnlyList<JobPosting>> FetchAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(_postings.Value);
    }

    private static IReadOnlyList<JobPosting> Load(Func<Stream?> openCatalogue)
    {
        using var stream = openCatalogue()
            ?? throw new InvalidOperationException(
                $"Demo vacancy catalogue resource '{ResourceName}' was not found in {typeof(StaticJobSource).Assembly.GetName().Name}.");

        List<CatalogueEntry>? entries;
        try
        {
            entries = JsonSerializer.Deserialize<List<CatalogueEntry>>(stream, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Demo vacancy catalogue '{ResourceName}' is not valid: {ex.Message}", ex);
        }

        if (entries is null || entries.Count == 0)
        {
            throw new InvalidOperationException($"Demo vacancy catalogue '{ResourceName}' contains no postings.");
        }

        return entries.Select(ToPosting).ToList().AsReadOnly();
    }

    private static JobPosting ToPosting(CatalogueEntry entry) =>
        new()
        {
            Title = entry.Title,
            Description = entry.Description,
            Company = entry.Company,
            CompanyWebsite = entry.CompanyWebsite,
            Location = entry.Location,
            WorkTimeType = entry.WorkTimeType,
            WorkLocationType = entry.WorkLocationType,
            JobCategories = entry.JobCategories ?? [],
            Skills = entry.Skills ?? [],
            SalaryMin = entry.SalaryMin,
            SalaryMax = entry.SalaryMax,
            Currency = entry.Currency,
            ExperienceLevel = entry.ExperienceLevel,
            SourceName = SourceName,
            SourceUrl = BaseUrl + entry.Slug,
        };

    /// <summary>Shape of one row in <c>demo-vacancies.json</c>.</summary>
    private sealed class CatalogueEntry
    {
        public required string Slug { get; init; }
        public required string Title { get; init; }
        public required string Company { get; init; }
        public required string Description { get; init; }
        public string? CompanyWebsite { get; init; }
        public Location? Location { get; init; }
        public WorkTimeType? WorkTimeType { get; init; }
        public WorkLocationType? WorkLocationType { get; init; }
        public string[]? JobCategories { get; init; }
        public string[]? Skills { get; init; }
        public decimal? SalaryMin { get; init; }
        public decimal? SalaryMax { get; init; }
        public Currency? Currency { get; init; }
        public string? ExperienceLevel { get; init; }
    }
}
