using JobNecto.Application.Interfaces;
using JobNecto.Domain.Entities;
using JobNecto.Domain.ValueObjects;

namespace JobNecto.Application.JobSources;

/// <summary>
/// Default <see cref="IVacancyIngestionService"/>: fetches from the registered <see cref="IJobSource"/>,
/// skips postings the user already has, and saves the rest once.
/// </summary>
public sealed class VacancyIngestionService : IVacancyIngestionService
{
    /// <summary>Gap between consecutive new postings, so the default newest-first sort is deterministic.</summary>
    internal static readonly TimeSpan CreatedAtStagger = TimeSpan.FromHours(6);

    private readonly IJobSource _jobSource;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="VacancyIngestionService"/> class.
    /// </summary>
    public VacancyIngestionService(IJobSource jobSource, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _jobSource = jobSource;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<int> IngestForUserAsync(Guid userId, CancellationToken ct)
    {
        var postings = await _jobSource.FetchAsync(ct);

        var knownUrls = new HashSet<string>(
            await _unitOfWork.VacancyRepository.GetJobSourceUrlsAsync(userId, ct),
            StringComparer.Ordinal);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var added = 0;

        foreach (var posting in postings)
        {
            // Add() also guards against a source listing the same URL twice.
            if (!knownUrls.Add(posting.SourceUrl))
            {
                continue;
            }

            // Only new postings consume a stagger slot, so an extended catalogue never shifts existing rows.
            var timestamp = now - (CreatedAtStagger * added);
            await _unitOfWork.VacancyRepository.CreateAsync(ToVacancy(posting, userId, timestamp), ct);
            added++;
        }

        if (added > 0)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return added;
    }

    private static Vacancy ToVacancy(JobPosting posting, Guid userId, DateTime timestamp) =>
        new()
        {
            UserId = userId,
            Title = posting.Title,
            Description = posting.Description,
            Company = posting.Company,
            CompanyWebsite = posting.CompanyWebsite,
            Location = posting.Location,
            WorkTimeType = posting.WorkTimeType,
            WorkLocationType = posting.WorkLocationType,
            JobCategories = [.. posting.JobCategories],
            Skills = [.. posting.Skills],
            SalaryMin = posting.SalaryMin,
            SalaryMax = posting.SalaryMax,
            Currency = posting.Currency,
            ExperienceLevel = posting.ExperienceLevel,
            JobSource = new JobSource { Name = posting.SourceName, Url = posting.SourceUrl },
            CreatedAt = timestamp,
            UpdatedAt = timestamp,
        };
}
