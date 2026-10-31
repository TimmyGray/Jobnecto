namespace JobNecto.Application.JobSources;

/// <summary>
/// A provider of job postings. The demo catalogue and any future real ingestion adapter implement
/// this contract, so swapping sources is a single DI registration.
/// </summary>
public interface IJobSource
{
    /// <summary>Display name of the source.</summary>
    string Name { get; }

    /// <summary>Returns the postings currently offered by this source.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The postings; never <c>null</c>.</returns>
    Task<IReadOnlyList<JobPosting>> FetchAsync(CancellationToken ct);
}
