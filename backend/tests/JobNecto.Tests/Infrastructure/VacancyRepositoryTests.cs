using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using JobNecto.Infrastructure.Persistance;
using JobNecto.Infrastructure.Repositories;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;
using JobNecto.Domain.ValueObjects;

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
    public async Task GetFilteredAsync_with_user_scope_returns_only_current_users_vacancies()
    {
        var (context, ownerId, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var otherUser = VacancyTestData.CreateOwnerUser(id: Guid.NewGuid());
            otherUser.Login = "vacancy-repo-tester-other";
            otherUser.Email = "vacancy.tests.other@example.com";
            context.Users.Add(otherUser);

            var otherUsersVacancy = VacancyTestData.JuniorReactHybridBerlin(
                otherUser.Id,
                VacancyTestData.AnchorUtc.AddDays(-1),
                VacancyTestData.AnchorUtc.AddHours(4)
            );
            context.Vacancies.Add(otherUsersVacancy);
            await context.SaveChangesAsync();

            var repo = new VacancyRepository(context);
            var result = await repo.GetFilteredAsync(
                new PagedQuery { UserId = ownerId, PageSize = 50 },
                filter: null,
                CancellationToken.None
            );

            result.TotalCount.Should().Be(7);
            result.Items.Should().OnlyContain(v => v.UserId == ownerId);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_orders_by_CreatedAt_desc_then_Id_and_pages()
    {
        var (context, _, seeded) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var expectedOrder = seeded.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id).ToList();

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
            result.LastSeenUpdatedAt.Should().Be(expectedOrder[1].CreatedAt);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_with_sort_by_updated_at_orders_by_UpdatedAt_desc_then_Id()
    {
        var (context, _, seeded) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var expectedOrder = seeded.OrderByDescending(v => v.UpdatedAt).ThenByDescending(v => v.Id).ToList();

            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 3, SortBy = "updatedAt" },
                filter: null,
                CancellationToken.None
            );

            result.TotalCount.Should().Be(7);
            result.Items.Should().HaveCount(3);
            result.Items[0].Id.Should().Be(expectedOrder[0].Id);
            result.Items[1].Id.Should().Be(expectedOrder[1].Id);
            result.Items[2].Id.Should().Be(expectedOrder[2].Id);
            result.LastSeenUpdatedAt.Should().Be(expectedOrder[2].UpdatedAt);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_cursor_page_returns_next_slice()
    {
        var (context, _, seeded) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var expectedOrder = seeded.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id).ToList();

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
    public async Task GetFilteredAsync_ties_on_same_created_at_are_paginated_deterministically_by_id()
    {
        await using var context = CreateContext();
        var owner = VacancyTestData.CreateOwnerUser();
        context.Users.Add(owner);

        var sharedCreatedAt = VacancyTestData.AnchorUtc;
        var v1 = VacancyTestData.SeniorDotNetRemotePoland(owner.Id, sharedCreatedAt, sharedCreatedAt);
        var v2 = VacancyTestData.JuniorReactHybridBerlin(owner.Id, sharedCreatedAt, sharedCreatedAt);
        context.Vacancies.AddRange(v1, v2);
        await context.SaveChangesAsync();

        var repo = new VacancyRepository(context);

        var firstPage = await repo.GetFilteredAsync(
            new PagedQuery { PageSize = 1 },
            filter: null,
            CancellationToken.None
        );

        firstPage.TotalCount.Should().Be(2);
        firstPage.Items.Should().HaveCount(1);
        firstPage.HasNext.Should().BeTrue();

        var secondPage = await repo.GetFilteredAsync(
            new PagedQuery
            {
                PageSize = 1,
                LastSeenId = firstPage.LastSeenId,
                LastSeenUpdatedAt = firstPage.LastSeenUpdatedAt,
            },
            filter: null,
            CancellationToken.None
        );

        secondPage.Items.Should().HaveCount(1);
        secondPage.HasNext.Should().BeFalse();

        // Both pages together cover all records with no duplicates or gaps.
        var allReturnedIds = new[] { firstPage.Items[0].Id, secondPage.Items[0].Id };
        allReturnedIds.Should().BeEquivalentTo(new[] { v1.Id, v2.Id });
        allReturnedIds.Distinct().Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFilteredAsync_exclude_keywords_removes_vacancies_whose_title_contains_keyword()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);

            // "PHP" appears in the title of LegacyFullStackHidden ("Full Stack Engineer (PHP + Vue)")
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { ExcludeKeywords = ["PHP"] },
                CancellationToken.None
            );

            result.Items.Should().NotContain(v => v.Title != null && v.Title.Contains("PHP"));
            result.TotalCount.Should().Be(6);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_exclude_keywords_removes_vacancies_whose_description_contains_keyword()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);

            // "WCAG" appears only in the description of JuniorReactHybridBerlin
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { ExcludeKeywords = ["WCAG"] },
                CancellationToken.None
            );

            result.Items.Should().NotContain(v => v.Description != null && v.Description.Contains("WCAG"));
            result.TotalCount.Should().Be(6);
        }
    }

    [Fact]
    public async Task GetFilteredAsync_multiple_exclude_keywords_apply_and_logic()
    {
        var (context, _, _) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);

            // "PHP" removes 1 vacancy; "WCAG" removes 1 more; combined removes 2
            var result = await repo.GetFilteredAsync(
                new PagedQuery { PageSize = 20 },
                new VacancyFilter { ExcludeKeywords = ["PHP", "WCAG"] },
                CancellationToken.None
            );

            result.TotalCount.Should().Be(5);
        }
    }

    [Fact]
    public async Task SoftDeleteAsync_SetsIsDeletedAndDeletedAtUtc()
    {
        var (context, _, seeded) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var vacancyId = seeded[0].Id;

            var entity = await repo.GetByIdAsync(vacancyId, CancellationToken.None);
            await repo.SoftDeleteAsync(entity, CancellationToken.None);

            entity.IsDeleted.Should().BeTrue();
            entity.DeletedAt.Should().NotBeNull();
            entity.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        }
    }

    [Fact]
    public async Task SoftDeleteAsync_AfterSaveChanges_EntityExcludedFromQuery()
    {
        var (context, _, seeded) = await SeedAllVacanciesAsync();
        await using (context)
        {
            var repo = new VacancyRepository(context);
            var vacancyId = seeded[0].Id;

            var entity = await repo.GetByIdAsync(vacancyId, CancellationToken.None);
            await repo.SoftDeleteAsync(entity, CancellationToken.None);
            await context.SaveChangesAsync();

            var results = await context.Vacancies.ToListAsync();
            results.Should().NotContain(v => v.Id == vacancyId);
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
