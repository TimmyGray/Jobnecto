using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using JobNecto.Application.JobSources;
using JobNecto.Domain.Enums;
using JobNecto.Infrastructure.JobSources.Static;

namespace JobNecto.Tests.Infrastructure.JobSources;

/// <summary>
/// Guards the synthetic demo catalogue: size, enum validity, salary sanity, URL rules, no personal data,
/// and enough variety for the vacancy filters and grounding to have hits.
/// </summary>
public class StaticJobSourceTests
{
    private static readonly string[] AllowedExperienceLevels = ["Junior", "Middle", "Senior", "Lead"];

    private static readonly string[] ResumeSeedSkills =
        ["Angular", "TypeScript", "C#", ".NET", "React", "Python", "SQL", "AWS", "Docker"];

    // Names of real brands and Microsoft sample companies that must never appear as employers.
    private static readonly string[] ForbiddenCompanyWords =
        ["Contoso", "Northwind", "Fabrikam", "Adventure Works", "Microsoft", "Google", "Amazon", "Meta", "Apple"];

    private static readonly Regex EmailPattern = new("@", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"\+?\d[\d\s-]{7,}", RegexOptions.Compiled);

    private static async Task<IReadOnlyList<JobPosting>> LoadCatalogueAsync() =>
        await new StaticJobSource().FetchAsync(CancellationToken.None);

    private static StaticJobSource SourceOver(string json) =>
        new(() => new MemoryStream(Encoding.UTF8.GetBytes(json)));

    [Fact]
    public void Name_IsTheDemoCatalogue()
    {
        new StaticJobSource().Name.Should().Be("Jobnecto Demo Catalogue");
    }

    [Fact]
    public async Task FetchAsync_ReturnsExactly40Postings()
    {
        var postings = await LoadCatalogueAsync();

        postings.Should().HaveCount(40);
    }

    [Fact]
    public async Task FetchAsync_CalledTwice_ReturnsTheSameCachedList()
    {
        var source = new StaticJobSource();

        var first = await source.FetchAsync(CancellationToken.None);
        var second = await source.FetchAsync(CancellationToken.None);

        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task FetchAsync_CancelledToken_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => new StaticJobSource().FetchAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Catalogue_EveryRowHasRequiredTextAndKnownEnumValues()
    {
        var postings = await LoadCatalogueAsync();

        postings.Should().AllSatisfy(p =>
        {
            p.Title.Should().NotBeNullOrWhiteSpace();
            p.Company.Should().NotBeNullOrWhiteSpace();
            p.Description.Should().NotBeNullOrWhiteSpace();
            p.Location.Should().NotBeNull();
            Enum.IsDefined(p.Location!.Value).Should().BeTrue();
            p.WorkTimeType.Should().NotBeNull();
            Enum.IsDefined(p.WorkTimeType!.Value).Should().BeTrue();
            p.WorkLocationType.Should().NotBeNull();
            Enum.IsDefined(p.WorkLocationType!.Value).Should().BeTrue();
            if (p.Currency is { } currency)
            {
                Enum.IsDefined(currency).Should().BeTrue();
            }
            p.ExperienceLevel.Should().BeOneOf(AllowedExperienceLevels);
            p.Skills.Should().NotBeEmpty();
            p.JobCategories.Should().NotBeEmpty();
        });
    }

    [Fact]
    public async Task Catalogue_FitsTheVacancyColumnLimits()
    {
        var postings = await LoadCatalogueAsync();

        postings.Should().AllSatisfy(p =>
        {
            p.Title.Length.Should().BeLessThanOrEqualTo(500);
            p.Description.Length.Should().BeLessThanOrEqualTo(5000);
            p.Company.Length.Should().BeLessThanOrEqualTo(100);
            (p.CompanyWebsite?.Length ?? 0).Should().BeLessThanOrEqualTo(500);
        });
    }

    [Fact]
    public async Task Catalogue_SalaryMinNeverExceedsSalaryMax_AndCurrencyGoesWithSalary()
    {
        var postings = await LoadCatalogueAsync();

        postings.Should().AllSatisfy(p =>
        {
            if (p.SalaryMin is { } min && p.SalaryMax is { } max)
            {
                min.Should().BeLessThanOrEqualTo(max);
            }
            (p.SalaryMin ?? 0).Should().BeGreaterThanOrEqualTo(0);
            (p.Currency is null).Should().Be(p.SalaryMin is null && p.SalaryMax is null);
        });
    }

    [Fact]
    public async Task Catalogue_UrlsAreUniqueAndUnderTheReservedTestDomain()
    {
        var postings = await LoadCatalogueAsync();

        postings.Select(p => p.SourceUrl).Should().OnlyHaveUniqueItems();
        postings.Should().AllSatisfy(p =>
        {
            p.SourceUrl.Should().StartWith("https://jobs.example.test/");
            Uri.IsWellFormedUriString(p.SourceUrl, UriKind.Absolute).Should().BeTrue();
            p.SourceName.Should().Be("Jobnecto Demo Catalogue");
            if (p.CompanyWebsite is not null)
            {
                new Uri(p.CompanyWebsite).Host.Should().EndWith(".example.test");
            }
        });
    }

    [Fact]
    public async Task Catalogue_DescriptionsContainNoEmailAddresses()
    {
        var postings = await LoadCatalogueAsync();

        postings.Should().AllSatisfy(p => EmailPattern.IsMatch(p.Description).Should().BeFalse(p.SourceUrl));
    }

    [Fact]
    public async Task Catalogue_DescriptionsContainNoPhoneNumbers()
    {
        var postings = await LoadCatalogueAsync();

        postings.Should().AllSatisfy(p => PhonePattern.IsMatch(p.Description).Should().BeFalse(p.SourceUrl));
    }

    [Fact]
    public async Task Catalogue_CompanyNamesAreInvented()
    {
        var postings = await LoadCatalogueAsync();

        postings.Should().AllSatisfy(p =>
            ForbiddenCompanyWords.Should().NotContain(w => p.Company.Contains(w, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Catalogue_CoversAllWorkLocationTypes()
    {
        var postings = await LoadCatalogueAsync();

        postings.Select(p => p.WorkLocationType!.Value).Distinct()
            .Should().BeEquivalentTo(Enum.GetValues<WorkLocationType>());
    }

    [Fact]
    public async Task Catalogue_CoversAtLeastFourWorkTimeTypes()
    {
        var postings = await LoadCatalogueAsync();

        postings.Select(p => p.WorkTimeType).Distinct().Count().Should().BeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task Catalogue_CoversAtLeastSixLocations()
    {
        var postings = await LoadCatalogueAsync();

        postings.Select(p => p.Location).Distinct().Count().Should().BeGreaterThanOrEqualTo(6);
    }

    [Fact]
    public async Task Catalogue_CoversAtLeastFourCurrencies()
    {
        var postings = await LoadCatalogueAsync();

        postings.Where(p => p.Currency is not null).Select(p => p.Currency).Distinct().Count()
            .Should().BeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task Catalogue_HasAtLeastFourRowsWithoutSalary()
    {
        var postings = await LoadCatalogueAsync();

        postings.Count(p => p.SalaryMin is null && p.SalaryMax is null).Should().BeGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task Catalogue_SkillsOverlapEveryResumeSeedSkill()
    {
        var postings = await LoadCatalogueAsync();
        var skills = postings.SelectMany(p => p.Skills).ToHashSet(StringComparer.Ordinal);

        skills.Should().Contain(ResumeSeedSkills);
    }

    [Fact]
    public async Task FetchAsync_MissingResource_ThrowsInvalidOperationNamingTheResource()
    {
        var source = new StaticJobSource(() => null);

        var act = () => source.FetchAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*demo-vacancies.json*not found*");
    }

    [Fact]
    public async Task FetchAsync_MalformedJson_ThrowsInvalidOperation()
    {
        var act = () => SourceOver("{ not json").FetchAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not valid*");
    }

    [Fact]
    public async Task FetchAsync_UnknownEnumValue_ThrowsInvalidOperation()
    {
        const string json = """
            [{ "slug": "x", "title": "T", "company": "C", "description": "D", "location": "Atlantis" }]
            """;

        var act = () => SourceOver(json).FetchAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not valid*");
    }

    [Fact]
    public async Task FetchAsync_IntegerEnumValue_ThrowsInvalidOperation()
    {
        const string json = """
            [{ "slug": "x", "title": "T", "company": "C", "description": "D", "location": 3 }]
            """;

        var act = () => SourceOver(json).FetchAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not valid*");
    }

    [Fact]
    public async Task Catalogue_FirstRow_MapsEveryField()
    {
        var posting = (await LoadCatalogueAsync())[0];

        posting.Should().BeEquivalentTo(new
        {
            Title = "Senior .NET Backend Engineer",
            Company = "Quillfern Analytics",
            CompanyWebsite = "https://quillfern.example.test",
            Location = (Location?)Location.Poland,
            WorkTimeType = (WorkTimeType?)WorkTimeType.FullTime,
            WorkLocationType = (WorkLocationType?)WorkLocationType.Remote,
            JobCategories = new[] { "Backend" },
            Skills = new[] { "C#", ".NET", "ASP.NET Core", "SQL", "PostgreSQL", "Docker", "Azure" },
            SalaryMin = (decimal?)260000m,
            SalaryMax = (decimal?)340000m,
            Currency = (Currency?)Currency.PLN,
            ExperienceLevel = "Senior",
            SourceName = "Jobnecto Demo Catalogue",
            SourceUrl = "https://jobs.example.test/demo/senior-dotnet-backend-engineer-quillfern",
        }, options => options.WithStrictOrdering());
        posting.Description.Should().StartWith("Quillfern Analytics builds reporting tools");
    }

    [Fact]
    public async Task FetchAsync_MissingRequiredField_ThrowsInvalidOperation()
    {
        const string json = """[{ "slug": "x", "title": "T", "company": "C" }]""";

        var act = () => SourceOver(json).FetchAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not valid*");
    }

    [Fact]
    public async Task FetchAsync_NullRequiredField_ThrowsInvalidOperation()
    {
        const string json = """[{ "slug": "x", "title": null, "company": "C", "description": "D" }]""";

        var act = () => SourceOver(json).FetchAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*not valid*");
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    public async Task FetchAsync_EmptyCatalogue_ThrowsInvalidOperation(string json)
    {
        var act = () => SourceOver(json).FetchAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no postings*");
    }

    [Fact]
    public async Task FetchAsync_MinimalRow_MapsOptionalCollectionsToEmpty()
    {
        const string json = """[{ "slug": "minimal", "title": "T", "company": "C", "description": "D" }]""";

        var postings = await SourceOver(json).FetchAsync(CancellationToken.None);

        var posting = postings.Should().ContainSingle().Subject;
        posting.SourceUrl.Should().Be("https://jobs.example.test/demo/minimal");
        posting.Skills.Should().BeEmpty();
        posting.JobCategories.Should().BeEmpty();
        posting.Location.Should().BeNull();
    }
}
