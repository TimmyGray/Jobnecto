using FluentAssertions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.JobSources;
using JobNecto.Infrastructure.JobSources.Seeding;
using JobNecto.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace JobNecto.Tests.Infrastructure.JobSources;

public class VacancySeedBackfillServiceTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IVacancyIngestionService> _ingestionMock = new();
    private readonly RecordingLogger<VacancySeedBackfillService> _logger = new();
    private int _ingestionScopes;

    public VacancySeedBackfillServiceTests()
    {
        _uowMock.Setup(x => x.UserRepository).Returns(_userRepoMock.Object);
    }

    private VacancySeedBackfillService CreateService(bool enabled)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _uowMock.Object);
        services.AddScoped(_ =>
        {
            _ingestionScopes++;
            return _ingestionMock.Object;
        });
        var provider = services.BuildServiceProvider();

        return new VacancySeedBackfillService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new VacancySeedOptions { Enabled = enabled }),
            _logger);
    }

    private void GivenUsers(params Guid[] ids) =>
        _userRepoMock.Setup(x => x.GetAllIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ids);

    [Fact]
    public async Task StartAsync_Disabled_TouchesNoRepository()
    {
        await CreateService(enabled: false).StartAsync(CancellationToken.None);

        _uowMock.VerifyNoOtherCalls();
        _ingestionMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task StartAsync_Enabled_IngestsForEachUserInItsOwnScope_AndLogsTotals()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        GivenUsers(first, second);
        _ingestionMock.Setup(x => x.IngestForUserAsync(first, It.IsAny<CancellationToken>())).ReturnsAsync(40);
        _ingestionMock.Setup(x => x.IngestForUserAsync(second, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        await CreateService(enabled: true).StartAsync(CancellationToken.None);

        _ingestionMock.Verify(x => x.IngestForUserAsync(first, It.IsAny<CancellationToken>()), Times.Once);
        _ingestionMock.Verify(x => x.IngestForUserAsync(second, It.IsAny<CancellationToken>()), Times.Once);
        _ingestionScopes.Should().Be(2);
        _logger.Entries.Should().ContainSingle(e =>
            e.Level == LogLevel.Information
            && e.Message.Contains("2 users")
            && e.Message.Contains("40 vacancies added")
            && e.Message.Contains("0 users failed"));
    }

    [Fact]
    public async Task StartAsync_OneUserFails_OthersStillIngest()
    {
        var failing = Guid.NewGuid();
        var healthy = Guid.NewGuid();
        GivenUsers(failing, healthy);
        _ingestionMock
            .Setup(x => x.IngestForUserAsync(failing, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        _ingestionMock.Setup(x => x.IngestForUserAsync(healthy, It.IsAny<CancellationToken>())).ReturnsAsync(40);

        await CreateService(enabled: true).StartAsync(CancellationToken.None);

        _ingestionMock.Verify(x => x.IngestForUserAsync(healthy, It.IsAny<CancellationToken>()), Times.Once);
        _logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Warning && e.Message.Contains(failing.ToString()) && e.Exception != null);
        _logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Information && e.Message.Contains("40 vacancies added") && e.Message.Contains("1 users failed"));
    }

    [Fact]
    public async Task StartAsync_ListingUsersFails_CompletesAndLogsWarning()
    {
        _userRepoMock
            .Setup(x => x.GetAllIdsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        var act = () => CreateService(enabled: true).StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning && e.Exception != null);
    }

    [Fact]
    public async Task StartAsync_StartupAborted_Propagates()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var userId = Guid.NewGuid();
        GivenUsers(userId);
        _ingestionMock
            .Setup(x => x.IngestForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var act = () => CreateService(enabled: true).StartAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StartAsync_SourceTimesOutWithoutStartupAbort_CountsUserAsFailed_AndCompletes()
    {
        var slow = Guid.NewGuid();
        var healthy = Guid.NewGuid();
        GivenUsers(slow, healthy);
        _ingestionMock
            .Setup(x => x.IngestForUserAsync(slow, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("HttpClient.Timeout elapsed"));
        _ingestionMock.Setup(x => x.IngestForUserAsync(healthy, It.IsAny<CancellationToken>())).ReturnsAsync(40);

        var act = () => CreateService(enabled: true).StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        _ingestionMock.Verify(x => x.IngestForUserAsync(healthy, It.IsAny<CancellationToken>()), Times.Once);
        _logger.Entries.Should().Contain(e => e.Level == LogLevel.Information && e.Message.Contains("1 users failed"));
    }

    [Fact]
    public async Task StartAsync_ListingUsersTimesOutWithoutStartupAbort_CompletesAndLogsWarning()
    {
        _userRepoMock
            .Setup(x => x.GetAllIdsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("command timeout"));

        var act = () => CreateService(enabled: true).StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
        _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task StopAsync_CompletesImmediately()
    {
        var act = () => CreateService(enabled: true).StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
