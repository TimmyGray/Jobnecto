using JobNecto.Application.Users;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobNecto.Application.JobSources;

/// <summary>
/// Seeds a newly registered user's vacancy board when <see cref="VacancySeedOptions.Enabled"/> is on.
/// </summary>
public sealed class SeedVacanciesOnUserRegisteredHandler : INotificationHandler<UserRegisteredNotification>
{
    private readonly IOptions<VacancySeedOptions> _options;
    private readonly IVacancyIngestionService _ingestionService;
    private readonly ILogger<SeedVacanciesOnUserRegisteredHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeedVacanciesOnUserRegisteredHandler"/> class.
    /// </summary>
    public SeedVacanciesOnUserRegisteredHandler(
        IOptions<VacancySeedOptions> options,
        IVacancyIngestionService ingestionService,
        ILogger<SeedVacanciesOnUserRegisteredHandler> logger)
    {
        _options = options;
        _ingestionService = ingestionService;
        _logger = logger;
    }

    /// <summary>
    /// Ingests the job source for the new user. Never throws except when <paramref name="cancellationToken"/>
    /// is cancelled: the account
    /// is already committed, so a seeding failure must not turn sign-up into an error.
    /// </summary>
    /// <param name="notification">The registration event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task Handle(UserRegisteredNotification notification, CancellationToken cancellationToken)
    {
        if (!_options.Value.Enabled)
        {
            return;
        }

        try
        {
            var added = await _ingestionService.IngestForUserAsync(notification.UserId, cancellationToken);
            _logger.LogInformation(
                "Seeded {AddedCount} demo vacancies for new user {UserId}.", added, notification.UserId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // MediatR re-throws notification handler exceptions, so isolation has to happen here.
            // Only a cancellation the caller asked for propagates; a source's own timeout is just a failure.
            _logger.LogWarning(
                ex, "Seeding demo vacancies failed for new user {UserId}; sign-up continues.", notification.UserId);
        }
    }
}
