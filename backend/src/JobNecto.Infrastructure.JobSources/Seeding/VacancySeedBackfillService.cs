using JobNecto.Application.Interfaces;
using JobNecto.Application.JobSources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobNecto.Infrastructure.JobSources.Seeding;

/// <summary>
/// On startup, when <see cref="VacancySeedOptions.Enabled"/> is on, ingests the job source for every
/// existing user. Ingestion is idempotent, so users who already have the catalogue gain nothing.
/// Never fails startup.
/// </summary>
public sealed class VacancySeedBackfillService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<VacancySeedOptions> _options;
    private readonly ILogger<VacancySeedBackfillService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VacancySeedBackfillService"/> class.
    /// </summary>
    public VacancySeedBackfillService(
        IServiceScopeFactory scopeFactory,
        IOptions<VacancySeedOptions> options,
        ILogger<VacancySeedBackfillService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Runs the backfill sequentially, one user at a time. A failure for one user is logged and
    /// the rest continue; a failure to list users is logged and startup proceeds.
    /// </summary>
    /// <param name="cancellationToken">Signals that host startup was aborted.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.Enabled)
        {
            return;
        }

        try
        {
            IReadOnlyList<Guid> userIds;
            await using (var scope = _scopeFactory.CreateAsyncScope())
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                userIds = await unitOfWork.UserRepository.GetAllIdsAsync(cancellationToken);
            }

            var added = 0;
            var failed = 0;
            foreach (var userId in userIds)
            {
                try
                {
                    added += await IngestInOwnScopeAsync(userId, cancellationToken);
                }
                catch (Exception ex) when (IsFailureRatherThanShutdown(ex, cancellationToken))
                {
                    failed++;
                    _logger.LogWarning(ex, "Demo vacancy backfill failed for user {UserId}; continuing.", userId);
                }
            }

            _logger.LogInformation(
                "Demo vacancy backfill checked {UserCount} users: {AddedCount} vacancies added, {FailedCount} users failed.",
                userIds.Count, added, failed);
        }
        catch (Exception ex) when (IsFailureRatherThanShutdown(ex, cancellationToken))
        {
            _logger.LogWarning(ex, "Demo vacancy backfill could not run; startup continues without it.");
        }
    }

    // Only an aborted startup propagates; a source's own timeout (e.g. an HttpClient
    // TaskCanceledException) is an ordinary failure and must not fail startup.
    private static bool IsFailureRatherThanShutdown(Exception ex, CancellationToken ct) =>
        ex is not OperationCanceledException || !ct.IsCancellationRequested;

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // A fresh scope per user gives each ingestion its own DbContext, so entities left tracked by
    // one user's failed save cannot leak into the next user's save.
    private async Task<int> IngestInOwnScopeAsync(Guid userId, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var ingestion = scope.ServiceProvider.GetRequiredService<IVacancyIngestionService>();
        return await ingestion.IngestForUserAsync(userId, ct);
    }
}
