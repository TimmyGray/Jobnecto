using FluentAssertions;
using JobNecto.Application.Interfaces;
using JobNecto.Application.JobSources;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;
using JobNecto.Tests.TestDoubles;
using Moq;

namespace JobNecto.Tests.Application.JobSources;

public class VacancyIngestionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IVacancyRepository> _vacancyRepoMock = new();
    private readonly Mock<IJobSource> _jobSourceMock = new();
    private readonly List<Vacancy> _created = [];
    private readonly Guid _userId = Guid.NewGuid();

    public VacancyIngestionServiceTests()
    {
        _uowMock.Setup(x => x.VacancyRepository).Returns(_vacancyRepoMock.Object);
        _vacancyRepoMock
            .Setup(x => x.CreateAsync(It.IsAny<Vacancy>(), It.IsAny<CancellationToken>()))
            .Callback<Vacancy, CancellationToken>((v, _) => _created.Add(v))
            .ReturnsAsync((Vacancy v, CancellationToken _) => v);
        GivenExistingUrls();
    }

    private VacancyIngestionService CreateService() =>
        new(_jobSourceMock.Object, _uowMock.Object, new FixedTimeProvider(Now));

    private void GivenPostings(params JobPosting[] postings) =>
        _jobSourceMock.Setup(x => x.FetchAsync(It.IsAny<CancellationToken>())).ReturnsAsync(postings);

    private void GivenExistingUrls(params string[] urls) =>
        _vacancyRepoMock
            .Setup(x => x.GetJobSourceUrlsAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(urls);

    private static JobPosting Posting(string slug) =>
        new()
        {
            Title = "Engineer " + slug,
            Description = "Description of " + slug,
            Company = "Company " + slug,
            CompanyWebsite = "https://company.example.test",
            Location = Location.Poland,
            WorkTimeType = WorkTimeType.FullTime,
            WorkLocationType = WorkLocationType.Remote,
            JobCategories = ["Backend"],
            Skills = ["C#", "SQL"],
            SalaryMin = 100,
            SalaryMax = 200,
            Currency = Currency.EUR,
            ExperienceLevel = "Senior",
            SourceName = "Test Source",
            SourceUrl = "https://jobs.example.test/demo/" + slug,
        };

    [Fact]
    public async Task IngestForUserAsync_NewUser_AddsEveryPostingMappedToTheUser()
    {
        var posting = Posting("a");
        GivenPostings(posting, Posting("b"), Posting("c"));

        var added = await CreateService().IngestForUserAsync(_userId, CancellationToken.None);

        added.Should().Be(3);
        _created.Should().HaveCount(3).And.OnlyContain(v => v.UserId == _userId);
        var first = _created[0];
        first.Title.Should().Be(posting.Title);
        first.Description.Should().Be(posting.Description);
        first.Company.Should().Be(posting.Company);
        first.CompanyWebsite.Should().Be(posting.CompanyWebsite);
        first.Location.Should().Be(Location.Poland);
        first.WorkTimeType.Should().Be(WorkTimeType.FullTime);
        first.WorkLocationType.Should().Be(WorkLocationType.Remote);
        first.JobCategories.Should().Equal("Backend");
        first.Skills.Should().Equal("C#", "SQL");
        first.SalaryMin.Should().Be(100);
        first.SalaryMax.Should().Be(200);
        first.Currency.Should().Be(Currency.EUR);
        first.ExperienceLevel.Should().Be("Senior");
        first.JobSource.Name.Should().Be("Test Source");
        first.JobSource.Url.Should().Be(posting.SourceUrl);
        first.MatchScore.Should().BeNull();
        first.IsViewed.Should().BeFalse();
        first.IsChosen.Should().BeFalse();
        first.IsHidden.Should().BeFalse();
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IngestForUserAsync_StaggersTimestampsSixHoursApartFromNow()
    {
        GivenPostings(Posting("a"), Posting("b"), Posting("c"));

        await CreateService().IngestForUserAsync(_userId, CancellationToken.None);

        var now = Now.UtcDateTime;
        _created.Select(v => v.CreatedAt).Should().Equal(now, now.AddHours(-6), now.AddHours(-12));
        _created.Should().OnlyContain(v => v.UpdatedAt == v.CreatedAt);
        _created.Should().OnlyContain(v => v.CreatedAt.Kind == DateTimeKind.Utc);
    }

    [Fact]
    public async Task IngestForUserAsync_SkipsExistingUrls_AndCountsOnlyNewOnes()
    {
        GivenPostings(Posting("a"), Posting("b"), Posting("c"));
        GivenExistingUrls("https://jobs.example.test/demo/b");

        var added = await CreateService().IngestForUserAsync(_userId, CancellationToken.None);

        added.Should().Be(2);
        _created.Select(v => v.JobSource.Url).Should().Equal(
            "https://jobs.example.test/demo/a", "https://jobs.example.test/demo/c");
    }

    [Fact]
    public async Task IngestForUserAsync_SkippedPostingsDoNotConsumeAStaggerSlot()
    {
        GivenPostings(Posting("a"), Posting("b"), Posting("c"));
        GivenExistingUrls("https://jobs.example.test/demo/a");

        await CreateService().IngestForUserAsync(_userId, CancellationToken.None);

        var now = Now.UtcDateTime;
        _created.Select(v => v.CreatedAt).Should().Equal(now, now.AddHours(-6));
    }

    [Fact]
    public async Task IngestForUserAsync_EverythingAlreadyExists_AddsNothingAndDoesNotSave()
    {
        GivenPostings(Posting("a"), Posting("b"));
        GivenExistingUrls("https://jobs.example.test/demo/a", "https://jobs.example.test/demo/b");

        var added = await CreateService().IngestForUserAsync(_userId, CancellationToken.None);

        added.Should().Be(0);
        _created.Should().BeEmpty();
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestForUserAsync_SourceListsSameUrlTwice_AddsItOnce()
    {
        GivenPostings(Posting("a"), Posting("a"));

        var added = await CreateService().IngestForUserAsync(_userId, CancellationToken.None);

        added.Should().Be(1);
        _created.Should().ContainSingle();
    }

    [Fact]
    public async Task IngestForUserAsync_EmptySource_AddsNothingAndDoesNotSave()
    {
        GivenPostings();

        var added = await CreateService().IngestForUserAsync(_userId, CancellationToken.None);

        added.Should().Be(0);
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestForUserAsync_SourceThrows_Propagates()
    {
        _jobSourceMock
            .Setup(x => x.FetchAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("source down"));

        var act = () => CreateService().IngestForUserAsync(_userId, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("source down");
        _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IngestForUserAsync_SaveThrows_Propagates()
    {
        GivenPostings(Posting("a"));
        _uowMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => CreateService().IngestForUserAsync(_userId, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
    }
}
