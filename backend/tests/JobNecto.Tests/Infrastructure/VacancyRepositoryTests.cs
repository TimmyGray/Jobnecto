using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Infrastructure.Repositories;

/// <summary>
/// Use <see cref="VacancyTestData"/> for seeds. InMemory does not translate <see cref="VacancyFilter.Skills"/> /
/// <see cref="VacancyFilter.JobCategories"/> queries — test those against PostgreSQL (e.g. Testcontainers), not here.
/// </summary>
public class VacancyRepositoryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext context, Guid ownerId, IReadOnlyList<Vacancy> seeded)> SeedAllVacanciesAsync()
    {
        var context = CreateContext();
        var owner = VacancyTestData.CreateOwnerUser();
        context.Users.Add(owner);
        var list = VacancyTestData.AllDistinctUpdatedAt(owner.Id).ToList();
        foreach (var v in list)
            context.Vacancies.Add(v);
        await context.SaveChangesAsync();
        return (context, owner.Id, list);
    }

    [Fact]
    public async Task GetFilteredAsync_with_company_filter_returns_single_vacancy()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { Company = "MedStack" },
                CancellationToken.None
            );
            result.Items.Should().ContainSingle();
            result.Items[0].Title.Should().Contain(".NET");
            result.TotalCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_returns_empty_result_when_no_vacancies_found()
    {
        await using var context = CreateContext();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { Company = "MedStack" },
                CancellationToken.None
            );
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_orders_by_UpdatedAt_desc_then_Id_and_pages()
    {
        var (context, _, seeded) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var expectedOrder = seeded.OrderByDescending(v => v.UpdatedAt).ThenByDescending(v => v.Id).ToList();

            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 2 },
                filter: null,
                CancellationToken.None
            );

            result.TotalCount.Should().Be(7);
            result.Items.Should().HaveCount(2);
            result.HasNext.Should().BeTrue();
            result.Items[0].Id.Should().Be(expectedOrder[0].Id);
            result.Items[1].Id.Should().Be(expectedOrder[1].Id);
            result.LastSeenId.Should().Be(expectedOrder[1].Id);
            result.LastSeenUpdatedAt.Should().Be(expectedOrder[1].UpdatedAt);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_cursor_page_returns_next_slice()
    {
        var (context, _, seeded) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var expectedOrder = seeded.OrderByDescending(v => v.UpdatedAt).ThenByDescending(v => v.Id).ToList();

            var first = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 2 },
                filter: null,
                CancellationToken.None
            );

            var second = await repo.GetFilteredAsync(
                new PagedQuery
                {
                    PageSize = 2,
                    LastSeenId = first.LastSeenId,
                    LastSeenUpdatedAt = first.LastSeenUpdatedAt,
                },
                filter: null,
                CancellationToken.None
            );

            second.Items.Should().HaveCount(2);
            second.Items[0].Id.Should().Be(expectedOrder[2].Id);
            second.Items[1].Id.Should().Be(expectedOrder[3].Id);
            second.TotalCount.Should().Be(7);
            second.HasNext.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetFilteredAsync_with_null_filter_returns_all_rows()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 50 },
                filter: null,
                CancellationToken.None
            );

            result.TotalCount.Should().Be(7);
            result.Items.Should().HaveCount(7);
            result.HasNext.Should().BeFalse();
        }
    }

    [Fact]
    public async Task GetFilteredAsync_whitespace_only_string_filters_are_ignored()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 50 },
                new VacancyFilter { Company = "   ", Title = "\t" },
                CancellationToken.None
            );

            result.TotalCount.Should().Be(7);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_title_filter_matches_like()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { Title = "React" },
                CancellationToken.None
            );

            result.TotalCount.Should().Be(1);
            result.Items[0].Title.Should().Contain("React");
        }
    }

    [Fact]
    public async Task GetFilteredAsync_description_filter_matches_like()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { Description = "WCAG" },
                CancellationToken.None
            );

            result.TotalCount.Should().Be(1);
            result.Items[0].Title.Should().Contain("Frontend");
        }
    }

    [Fact]
    public async Task GetFilteredAsync_location_filter_matches_any_enum()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { Location = [Location.Poland] },
                CancellationToken.None
            );

            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(v => v.Location == Location.Poland);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_work_time_type_filter_matches()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { WorkTimeType = [WorkTimeType.Contract] },
                CancellationToken.None
            );

            result.TotalCount.Should().Be(1);
            result.Items[0].WorkTimeType.Should().Be(WorkTimeType.Contract);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_work_location_type_filter_matches()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { WorkLocationType = [WorkLocationType.Hybrid] },
                CancellationToken.None
            );

            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(v => v.WorkLocationType == WorkLocationType.Hybrid);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_salary_min_requires_vacancy_min_at_least_filter()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { SalaryMin = 95_000m },
                CancellationToken.None
            );

            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(v => v.SalaryMin >= 95_000m);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_salary_max_requires_vacancy_max_at_most_filter()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { SalaryMax = 56_000m },
                CancellationToken.None
            );

            result.Items.Should().OnlyContain(v => v.SalaryMax <= 56_000m);
            result.TotalCount.Should().Be(3);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_currency_filter_matches()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { Currency = [Currency.EUR] },
                CancellationToken.None
            );

            result.TotalCount.Should().Be(3);
            result.Items.Should().OnlyContain(v => v.Currency == Currency.EUR);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_match_score_min_inclusive()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { MatchScore = 0.88 },
                CancellationToken.None
            );

            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(v => v.MatchScore >= 0.88);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_experience_level_filter_matches()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { ExperienceLevel = ["Senior"] },
                CancellationToken.None
            );

            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(v => v.ExperienceLevel == "Senior");
        }
    }

    [Fact]
    public async Task GetFilteredAsync_job_source_name_filter_matches()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { JobSource = ["LinkedIn"] },
                CancellationToken.None
            );

            result.TotalCount.Should().Be(2);
            result.Items.Should().OnlyContain(v => v.JobSource.Name == "LinkedIn");
        }
    }

    [Fact]
    public async Task GetFilteredAsync_is_chosen_filter_matches()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { IsChosen = true },
                CancellationToken.None
            );

            result.TotalCount.Should().Be(1);
            result.Items[0].IsChosen.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetFilteredAsync_created_at_min_inclusive()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var cutoff = VacancyTestData.AnchorUtc.AddDays(-7);
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { CreatedAt = cutoff },
                CancellationToken.None
            );

            result.Items.Should().OnlyContain(v => v.CreatedAt >= cutoff);
            result.TotalCount.Should().Be(3);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_updated_at_min_inclusive()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var cutoff = VacancyTestData.AnchorUtc.AddHours(3);
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { UpdatedAt = cutoff },
                CancellationToken.None
            );

            result.Items.Should().OnlyContain(v => v.UpdatedAt >= cutoff);
            result.TotalCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_page_size_zero_is_treated_as_one()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 0 },
                filter: null,
                CancellationToken.None
            );

            result.Items.Should().HaveCount(1);
            result.PageSize.Should().Be(0);
            result.HasNext.Should().BeTrue();
        }
    }

    [Fact]
    public async Task UpdateMatchScoreAsync_throws_NotImplementedException()
    {
        await using var context = CreateContext();
        var repo = new VacancyRepository(context);
        var act = async () =>
            await repo.UpdateMatchScoreAsync(Guid.NewGuid(), 0.5, CancellationToken.None);
        await act.Should().ThrowAsync<NotImplementedException>();
    }
}
