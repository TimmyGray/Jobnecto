using FluentAssertions;
using JobNecto.Application.JobSources;
using JobNecto.Application.Users;
using JobNecto.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace JobNecto.Tests.Application.JobSources;

public class SeedVacanciesOnUserRegisteredHandlerTests
{
    private readonly Mock<IVacancyIngestionService> _ingestionMock = new();
    private readonly RecordingLogger<SeedVacanciesOnUserRegisteredHandler> _logger = new();
    private readonly UserRegisteredNotification _notification = new(Guid.NewGuid());

    private SeedVacanciesOnUserRegisteredHandler CreateHandler(bool enabled) =>
        new(Options.Create(new VacancySeedOptions { Enabled = enabled }), _ingestionMock.Object, _logger);

    [Fact]
    public async Task Handle_Disabled_NeverCallsIngestion()
    {
        await CreateHandler(enabled: false).Handle(_notification, CancellationToken.None);

        _ingestionMock.Verify(
            x => x.IngestForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Enabled_IngestsForTheNotifiedUser()
    {
        using var cts = new CancellationTokenSource();
        _ingestionMock.Setup(x => x.IngestForUserAsync(_notification.UserId, cts.Token)).ReturnsAsync(40);

        await CreateHandler(enabled: true).Handle(_notification, cts.Token);

        _ingestionMock.Verify(x => x.IngestForUserAsync(_notification.UserId, cts.Token), Times.Once);
        _logger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Information
            && e.Message.Contains("40")
            && e.Message.Contains(_notification.UserId.ToString()));
    }

    [Fact]
    public async Task Handle_IngestionThrows_DoesNotThrow_AndLogsWarningWithUserId()
    {
        var failure = new InvalidOperationException("db down");
        _ingestionMock
            .Setup(x => x.IngestForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure);

        var act = () => CreateHandler(enabled: true).Handle(_notification, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _logger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Warning
            && e.Exception == failure
            && e.Message.Contains(_notification.UserId.ToString()));
    }

    [Fact]
    public async Task Handle_CallerCancelled_PropagatesOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _ingestionMock
            .Setup(x => x.IngestForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var act = () => CreateHandler(enabled: true).Handle(_notification, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _logger.Entries.Should().NotContain(e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task Handle_SourceTimesOutWithoutCallerCancellation_IsTreatedAsFailure()
    {
        var timeout = new TaskCanceledException("HttpClient.Timeout elapsed");
        _ingestionMock
            .Setup(x => x.IngestForUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(timeout);

        var act = () => CreateHandler(enabled: true).Handle(_notification, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Exception == timeout);
    }
}
