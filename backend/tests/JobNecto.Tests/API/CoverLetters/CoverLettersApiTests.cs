using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using JobNecto.Application.Users;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;
using JobNecto.Domain.ValueObjects;
using JobNecto.Infrastructure.Persistance;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobNecto.Tests.API.CoverLetters;

public class CoverLettersApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static string ValidContent() => new string('a', 50);

    private static CreateUserCommand NewUserCommand(string prefix = "cl_user") =>
        new()
        {
            LoginName = prefix + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "Password123!",
        };

    internal static async Task<(string AuthCookie, Guid UserId)> CreateUserAndGetCookieAsync(
        HttpClient client,
        CreateUserCommand? command = null)
    {
        command ??= NewUserCommand();

        var response = await client.PostAsJsonAsync("/api/v1/users", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdUser = await response.Content.ReadFromJsonAsync<CreateUserResult>(JsonOptions);
        createdUser.Should().NotBeNull();

        var authCookie = response
            .Headers.GetValues("Set-Cookie")
            .Select(x =>
                x.Split(';', StringSplitOptions.TrimEntries)
                    .FirstOrDefault(y =>
                        y.StartsWith("auth-token=", StringComparison.OrdinalIgnoreCase)))
            .First(x => !string.IsNullOrWhiteSpace(x));

        authCookie.Should().NotBeNullOrWhiteSpace();
        return (authCookie!, createdUser!.Id);
    }

    internal static async Task<Guid> SeedVacancyAsync(
        WebApplicationFactory<Program> factory,
        Guid userId,
        Guid? vacancyId = null,
        string? title = null,
        string? company = null,
        Location? location = null,
        WorkLocationType? workLocationType = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Users.AnyAsync(u => u.Id == userId))
        {
            db.Users.Add(new User
            {
                Id = userId,
                Login = "seed_" + Guid.NewGuid().ToString("N")[..8],
                Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
                Password = "seeded-password-hash",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        var now = DateTime.UtcNow;
        var vacancy = new Vacancy
        {
            Id = vacancyId ?? Guid.NewGuid(),
            UserId = userId,
            Title = title ?? "Backend Engineer",
            Company = company,
            Location = location,
            WorkLocationType = workLocationType,
            JobSource = new JobSource { Name = "LinkedIn", Url = "https://linkedin.com/jobs" },
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Vacancies.Add(vacancy);
        await db.SaveChangesAsync();

        return vacancy.Id;
    }

    internal static async Task<HttpResponseMessage> PostCoverLetterAsync(
        HttpClient client,
        string authCookie,
        object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/cover-letters")
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);

        return await client.SendAsync(request);
    }

    internal static async Task<Guid> SeedCoverLetterAsync(
        WebApplicationFactory<Program> factory,
        Guid userId,
        Guid vacancyId,
        DateTime createdAt,
        string? content = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var coverLetter = new CoverLetter
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VacancyId = vacancyId,
            Content = content ?? ValidContent(),
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };

        db.CoverLetters.Add(coverLetter);
        await db.SaveChangesAsync();

        return coverLetter.Id;
    }

    private static async Task<HttpResponseMessage> GetCoverLettersAsync(
        HttpClient client,
        string authCookie,
        string queryString = "")
    {
        var url = "/api/v1/cover-letters" +
                  (string.IsNullOrEmpty(queryString) ? "" : "?" + queryString);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> GetCoverLetterByIdAsync(
        HttpClient client,
        string authCookie,
        Guid id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/cover-letters/{id}");
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PatchCoverLetterAsync(
        HttpClient client,
        string authCookie,
        Guid id,
        object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/cover-letters/{id}")
        {
            Content = JsonContent.Create(payload),
        };

        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> DeleteCoverLetterAsync(
        HttpClient client,
        string authCookie,
        Guid id)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/cover-letters/{id}");
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/cover-letters",
            new { vacancyId = Guid.NewGuid(), content = ValidContent() });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ValidPayload_Returns201WithLocationAndPayload()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var vacancyId = await SeedVacancyAsync(factory, userId);

        var response = await PostCoverLetterAsync(client, authCookie, new
        {
            vacancyId,
            content = ValidContent(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var locationStr = response.Headers.Location!.ToString();
        locationStr.Should().StartWith("/api/v1/cover-letters/");
        Guid.TryParse(locationStr.Split('/').Last(), out _)
            .Should().BeTrue("Location header must end with a valid GUID");

        var result = await response.Content.ReadFromJsonAsync<CreateCoverLetterResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.VacancyId.Should().Be(vacancyId);
        result.Content.Should().Be(ValidContent());
        result.CreatedAt.Should().NotBe(default(DateTime));
        result.UpdatedAt.Should().NotBe(default(DateTime));
    }

    [Fact]
    public async Task Create_ContentLessThan50Chars_Returns400WithFieldLevelError()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var vacancyId = await SeedVacancyAsync(factory, userId);

        var response = await PostCoverLetterAsync(client, authCookie, new
        {
            vacancyId,
            content = new string('a', 49),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Content");
    }

    [Fact]
    public async Task Create_ContentMoreThan10000Chars_Returns400WithFieldLevelError()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var vacancyId = await SeedVacancyAsync(factory, userId);

        var response = await PostCoverLetterAsync(client, authCookie, new
        {
            vacancyId,
            content = new string('a', 10001),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Content");
    }

    [Fact]
    public async Task Create_NonExistingVacancy_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await CreateUserAndGetCookieAsync(client);

        var response = await PostCoverLetterAsync(client, authCookie, new
        {
            vacancyId = Guid.NewGuid(),
            content = ValidContent(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_VacancyOwnedByAnotherUser_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (_, ownerUserId) = await CreateUserAndGetCookieAsync(client, NewUserCommand("owner_"));
        var (requesterCookie, _) = await CreateUserAndGetCookieAsync(client, NewUserCommand("requester_"));

        var otherUsersVacancyId = await SeedVacancyAsync(factory, ownerUserId);

        var response = await PostCoverLetterAsync(client, requesterCookie, new
        {
            vacancyId = otherUsersVacancyId,
            content = ValidContent(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/cover-letters");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_EmptyLibrary_Returns200WithEmptyResult()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await CreateUserAndGetCookieAsync(client);

        var response = await GetCoverLettersAsync(client, authCookie);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoverLetterPagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(0);
        result.HasNext.Should().BeFalse();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReturnsCreatedAtDescending_WithVacancyTitles()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);

        var vacancy1 = await SeedVacancyAsync(factory, userId, title: "Role A");
        var vacancy2 = await SeedVacancyAsync(factory, userId, title: "Role B");
        var vacancy3 = await SeedVacancyAsync(factory, userId, title: "Role C");

        var now = DateTime.UtcNow;
        var oldestId = await SeedCoverLetterAsync(factory, userId, vacancy1, now.AddHours(-3), new string('a', 60));
        var newestId = await SeedCoverLetterAsync(factory, userId, vacancy2, now.AddHours(-1), new string('b', 60));
        var middleId = await SeedCoverLetterAsync(factory, userId, vacancy3, now.AddHours(-2), new string('c', 60));

        var response = await GetCoverLettersAsync(client, authCookie);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoverLetterPagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);

        result.Items[0].Id.Should().Be(newestId);
        result.Items[1].Id.Should().Be(middleId);
        result.Items[2].Id.Should().Be(oldestId);

        result.Items.Select(i => i.VacancyTitle).Should().Contain(["Role A", "Role B", "Role C"]);
    }

    [Fact]
    public async Task List_PageSizeAbove100_IsCappedTo100()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await CreateUserAndGetCookieAsync(client);

        var response = await GetCoverLettersAsync(client, authCookie, "pageSize=500");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoverLetterPagedResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task List_WithCursor_ReturnsNextSlice()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var now = DateTime.UtcNow;

        var v1 = await SeedVacancyAsync(factory, userId, title: "Role 1");
        var v2 = await SeedVacancyAsync(factory, userId, title: "Role 2");
        var v3 = await SeedVacancyAsync(factory, userId, title: "Role 3");
        var v4 = await SeedVacancyAsync(factory, userId, title: "Role 4");

        var expectedOrder = new List<Guid>
        {
            await SeedCoverLetterAsync(factory, userId, v1, now.AddHours(-1), new string('a', 60)),
            await SeedCoverLetterAsync(factory, userId, v2, now.AddHours(-2), new string('b', 60)),
            await SeedCoverLetterAsync(factory, userId, v3, now.AddHours(-3), new string('c', 60)),
            await SeedCoverLetterAsync(factory, userId, v4, now.AddHours(-4), new string('d', 60)),
        };

        var firstResponse = await GetCoverLettersAsync(client, authCookie, "pageSize=2");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstPage = await firstResponse.Content.ReadFromJsonAsync<CoverLetterPagedResultDto>(JsonOptions);
        firstPage.Should().NotBeNull();
        firstPage!.Items.Should().HaveCount(2);
        firstPage.Items[0].Id.Should().Be(expectedOrder[0]);
        firstPage.Items[1].Id.Should().Be(expectedOrder[1]);
        firstPage.HasNext.Should().BeTrue();
        firstPage.LastSeenId.Should().NotBeNull();
        firstPage.LastSeenUpdatedAt.Should().NotBeNull();

        var lastSeenTimestamp = Uri.EscapeDataString(firstPage.LastSeenUpdatedAt!.Value.ToString("o"));
        var secondResponse = await GetCoverLettersAsync(
            client,
            authCookie,
            $"pageSize=2&lastSeenId={firstPage.LastSeenId}&lastSeenUpdatedAt={lastSeenTimestamp}");

        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondPage = await secondResponse.Content.ReadFromJsonAsync<CoverLetterPagedResultDto>(JsonOptions);
        secondPage.Should().NotBeNull();
        secondPage!.Items.Should().HaveCount(2);
        secondPage.Items[0].Id.Should().Be(expectedOrder[2]);
        secondPage.Items[1].Id.Should().Be(expectedOrder[3]);
    }

    [Fact]
    public async Task List_AnotherUsersCoverLetters_AreNotVisible()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (cookieA, userA) = await CreateUserAndGetCookieAsync(client, NewUserCommand("cla_"));
        var (cookieB, userB) = await CreateUserAndGetCookieAsync(client, NewUserCommand("clb_"));

        var vacancyA = await SeedVacancyAsync(factory, userA, title: "Role A");
        var vacancyB = await SeedVacancyAsync(factory, userB, title: "Role B");

        await SeedCoverLetterAsync(factory, userA, vacancyA, DateTime.UtcNow.AddHours(-1), new string('a', 60));
        await SeedCoverLetterAsync(factory, userB, vacancyB, DateTime.UtcNow.AddHours(-1), new string('b', 60));

        var responseA = await GetCoverLettersAsync(client, cookieA);
        responseA.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultA = await responseA.Content.ReadFromJsonAsync<CoverLetterPagedResultDto>(JsonOptions);

        var responseB = await GetCoverLettersAsync(client, cookieB);
        responseB.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultB = await responseB.Content.ReadFromJsonAsync<CoverLetterPagedResultDto>(JsonOptions);

        resultA.Should().NotBeNull();
        resultB.Should().NotBeNull();
        resultA!.TotalCount.Should().Be(1);
        resultB!.TotalCount.Should().Be(1);
        resultA.Items[0].VacancyTitle.Should().Be("Role A");
        resultB.Items[0].VacancyTitle.Should().Be("Role B");
    }

    [Fact]
    public async Task List_WithOnlyLastSeenId_Returns400ProblemDetails()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await CreateUserAndGetCookieAsync(client);

        var response = await GetCoverLettersAsync(client, authCookie, $"lastSeenId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Validation failed");
    }

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/cover-letters/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_OwnedCoverLetter_Returns200WithNestedVacancy()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var vacancyId = await SeedVacancyAsync(
            factory,
            userId,
            title: "Senior Backend Engineer",
            company: "Contoso",
            location: Location.Turkey,
            workLocationType: WorkLocationType.Hybrid);

        var coverLetterId = await SeedCoverLetterAsync(
            factory,
            userId,
            vacancyId,
            DateTime.UtcNow.AddHours(-1),
            new string('z', 120));

        var response = await GetCoverLetterByIdAsync(client, authCookie, coverLetterId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoverLetterDetailResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().Be(coverLetterId);
        result.Content.Should().Be(new string('z', 120));
        result.VacancyId.Should().Be(vacancyId);
        result.Vacancy.Should().NotBeNull();
        result.Vacancy.Id.Should().Be(vacancyId);
        result.Vacancy.Title.Should().Be("Senior Backend Engineer");
        result.Vacancy.Company.Should().Be("Contoso");
        result.Vacancy.WorkLocationType.Should().Be("Hybrid");
        result.Vacancy.Location.Should().Be("Turkey");
    }

    [Fact]
    public async Task GetById_WhenVacancySoftDeleted_StillReturnsCoverLetter()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var vacancyId = await SeedVacancyAsync(
            factory,
            userId,
            title: "Soft Deleted Vacancy",
            company: "Contoso",
            location: Location.Turkey,
            workLocationType: WorkLocationType.Remote);

        var coverLetterId = await SeedCoverLetterAsync(
            factory,
            userId,
            vacancyId,
            DateTime.UtcNow.AddHours(-1),
            new string('w', 100));

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var vacancy = await db.Vacancies.IgnoreQueryFilters().SingleAsync(v => v.Id == vacancyId);
            vacancy.IsDeleted = true;
            vacancy.DeletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var response = await GetCoverLetterByIdAsync(client, authCookie, coverLetterId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoverLetterDetailResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().Be(coverLetterId);
        result.VacancyId.Should().Be(vacancyId);
        result.Vacancy.Id.Should().Be(vacancyId);
    }

    [Fact]
    public async Task GetById_NonExistentId_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await CreateUserAndGetCookieAsync(client);

        var response = await GetCoverLetterByIdAsync(client, authCookie, Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_AnotherUsersCoverLetter_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (_, ownerUserId) = await CreateUserAndGetCookieAsync(client, NewUserCommand("cl_detail_owner_"));
        var (requesterCookie, _) = await CreateUserAndGetCookieAsync(client, NewUserCommand("cl_detail_requester_"));

        var vacancyId = await SeedVacancyAsync(factory, ownerUserId, title: "Owner Vacancy");
        var coverLetterId = await SeedCoverLetterAsync(
            factory,
            ownerUserId,
            vacancyId,
            DateTime.UtcNow.AddHours(-1),
            new string('x', 80));

        var response = await GetCoverLetterByIdAsync(client, requesterCookie, coverLetterId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/v1/cover-letters/{Guid.NewGuid()}",
            new { content = new string('a', 60) });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Patch_OwnedCoverLetter_ContentOnly_Returns200AndRefreshesUpdatedAt()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var vacancyId = await SeedVacancyAsync(factory, userId, title: "Patch Vacancy");
        var coverLetterId = await SeedCoverLetterAsync(
            factory,
            userId,
            vacancyId,
            DateTime.UtcNow.AddHours(-2),
            new string('a', 80));

        var detailBeforeResponse = await GetCoverLetterByIdAsync(client, authCookie, coverLetterId);
        detailBeforeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailBefore = await detailBeforeResponse.Content.ReadFromJsonAsync<CoverLetterDetailResultDto>(JsonOptions);

        var response = await PatchCoverLetterAsync(client, authCookie, coverLetterId, new
        {
            content = new string('b', 120),
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoverLetterUpdateResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.Id.Should().Be(coverLetterId);
        result.VacancyId.Should().Be(vacancyId);
        result.Content.Should().Be(new string('b', 120));
        result.UpdatedAt.Should().BeAfter(detailBefore!.UpdatedAt);
    }

    [Fact]
    public async Task Patch_WithVacancyIdInBody_IsIgnored()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var originalVacancyId = await SeedVacancyAsync(factory, userId, title: "Immutable Vacancy");
        var coverLetterId = await SeedCoverLetterAsync(
            factory,
            userId,
            originalVacancyId,
            DateTime.UtcNow.AddHours(-1),
            new string('c', 90));

        var response = await PatchCoverLetterAsync(client, authCookie, coverLetterId, new
        {
            content = new string('d', 95),
            vacancyId = Guid.NewGuid(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CoverLetterUpdateResultDto>(JsonOptions);
        result.Should().NotBeNull();
        result!.VacancyId.Should().Be(originalVacancyId);
        result.Content.Should().Be(new string('d', 95));

        var detailResponse = await GetCoverLetterByIdAsync(client, authCookie, coverLetterId);
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detailResult = await detailResponse.Content.ReadFromJsonAsync<CoverLetterDetailResultDto>(JsonOptions);
        detailResult.Should().NotBeNull();
        detailResult!.VacancyId.Should().Be(originalVacancyId);
    }

    [Fact]
    public async Task Patch_InvalidContentBounds_Returns400()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client);
        var vacancyId = await SeedVacancyAsync(factory, userId, title: "Validation Vacancy");
        var coverLetterId = await SeedCoverLetterAsync(factory, userId, vacancyId, DateTime.UtcNow.AddHours(-1), new string('a', 70));

        var response = await PatchCoverLetterAsync(client, authCookie, coverLetterId, new
        {
            content = new string('x', 49),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Content");
    }

    [Fact]
    public async Task Patch_AnotherUsersCoverLetter_Returns403()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (_, ownerUserId) = await CreateUserAndGetCookieAsync(client, NewUserCommand("cl_patch_owner_"));
        var (attackerCookie, _) = await CreateUserAndGetCookieAsync(client, NewUserCommand("cl_patch_attacker_"));

        var vacancyId = await SeedVacancyAsync(factory, ownerUserId, title: "Owner Vacancy");
        var coverLetterId = await SeedCoverLetterAsync(factory, ownerUserId, vacancyId, DateTime.UtcNow.AddHours(-1), new string('a', 80));

        var response = await PatchCoverLetterAsync(client, attackerCookie, coverLetterId, new
        {
            content = new string('p', 85),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Patch_NonExistentId_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await CreateUserAndGetCookieAsync(client);

        var response = await PatchCoverLetterAsync(client, authCookie, Guid.NewGuid(), new
        {
            content = new string('q', 88),
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/v1/cover-letters/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_OwnedCoverLetter_Returns204()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client, NewUserCommand("cl_del_"));
        var vacancyId = await SeedVacancyAsync(factory, userId, title: "Delete Vacancy");
        var coverLetterId = await SeedCoverLetterAsync(factory, userId, vacancyId, DateTime.UtcNow.AddHours(-1), new string('a', 80));

        var response = await DeleteCoverLetterAsync(client, authCookie, coverLetterId);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistentId_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, _) = await CreateUserAndGetCookieAsync(client, NewUserCommand("cl_del_nf_"));

        var response = await DeleteCoverLetterAsync(client, authCookie, Guid.NewGuid());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AnotherUsersCoverLetter_Returns403()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (_, ownerUserId) = await CreateUserAndGetCookieAsync(client, NewUserCommand("cl_del_owner_"));
        var (attackerCookie, _) = await CreateUserAndGetCookieAsync(client, NewUserCommand("cl_del_attacker_"));

        var vacancyId = await SeedVacancyAsync(factory, ownerUserId, title: "Owner Vacancy");
        var coverLetterId = await SeedCoverLetterAsync(factory, ownerUserId, vacancyId, DateTime.UtcNow.AddHours(-1), new string('a', 70));

        var response = await DeleteCoverLetterAsync(client, attackerCookie, coverLetterId);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_OwnedCoverLetter_ExcludedFromListAfterDeletion()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client, NewUserCommand("cl_del_list_"));
        var vacancyId = await SeedVacancyAsync(factory, userId, title: "List Vacancy");
        var coverLetterId = await SeedCoverLetterAsync(factory, userId, vacancyId, DateTime.UtcNow.AddHours(-1), new string('b', 75));

        var deleteResponse = await DeleteCoverLetterAsync(client, authCookie, coverLetterId);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await GetCoverLettersAsync(client, authCookie);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listResult = await listResponse.Content.ReadFromJsonAsync<CoverLetterPagedResultDto>(JsonOptions);
        listResult.Should().NotBeNull();
        listResult!.Items.Should().BeEmpty();
        listResult.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Delete_OwnedCoverLetter_DetailReturns404AfterDeletion()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (authCookie, userId) = await CreateUserAndGetCookieAsync(client, NewUserCommand("cl_del_detail_"));
        var vacancyId = await SeedVacancyAsync(factory, userId, title: "Detail Vacancy");
        var coverLetterId = await SeedCoverLetterAsync(factory, userId, vacancyId, DateTime.UtcNow.AddHours(-1), new string('c', 85));

        var deleteResponse = await DeleteCoverLetterAsync(client, authCookie, coverLetterId);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detailResponse = await GetCoverLetterByIdAsync(client, authCookie, coverLetterId);
        detailResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    internal class CreateCoverLetterResultDto
    {
        public Guid Id { get; set; }
        public Guid VacancyId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    internal class CoverLetterPagedResultDto
    {
        public int TotalCount { get; set; }
        public int PageSize { get; set; }
        public bool HasNext { get; set; }
        public Guid? LastSeenId { get; set; }
        public DateTime? LastSeenUpdatedAt { get; set; }
        public List<CoverLetterListItemDto> Items { get; set; } = [];
    }

    internal class CoverLetterListItemDto
    {
        public Guid Id { get; set; }
        public Guid VacancyId { get; set; }
        public string? VacancyTitle { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    internal class CoverLetterDetailResultDto
    {
        public Guid Id { get; set; }
        public Guid VacancyId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public CoverLetterVacancyDto Vacancy { get; set; } = null!;
    }

    internal class CoverLetterVacancyDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Company { get; set; }
        public string? WorkLocationType { get; set; }
        public string? Location { get; set; }
    }

    internal class CoverLetterUpdateResultDto
    {
        public Guid Id { get; set; }
        public Guid VacancyId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}