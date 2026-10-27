namespace JobNecto.Application.JobSources;

/// <summary>
/// Turns the postings of the registered <see cref="IJobSource"/> into vacancies owned by one user.
/// </summary>
public interface IVacancyIngestionService
{
    /// <summary>
    /// Adds every posting the user does not already have (matched by source URL, including
    /// vacancies the user deleted) and saves them in one unit of work.
    /// Failures propagate; callers decide how to isolate them.
    /// </summary>
    /// <param name="userId">Owner of the new vacancies.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of vacancies added; 0 when the user already has every posting.</returns>
    Task<int> IngestForUserAsync(Guid userId, CancellationToken ct);
}
