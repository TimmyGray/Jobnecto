using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using JobNecto.Application.Users;
using JobNecto.Domain.Entities;
using JobNecto.Domain.Enums;
using JobNecto.Domain.ValueObjects;
using JobNecto.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobNecto.Tests.API.Authorization;

/// <summary>
/// Centralized fixtures for the cross-user authorization regression suite (Story R.3).
/// </summary>
internal static class AuthorizationTestFixture
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    internal readonly record struct UserContext(string AuthCookie, Guid UserId);

    internal static CreateUserCommand NewUserCommand(string prefix = "auth_user") =>
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

    internal static async Task<(UserContext A, UserContext B)> CreateTwoUsersAsync(HttpClient client)
    {
        var (cookieA, userA) = await CreateUserAndGetCookieAsync(client, NewUserCommand("auth_a_"));
        var (cookieB, userB) = await CreateUserAndGetCookieAsync(client, NewUserCommand("auth_b_"));

        return (new UserContext(cookieA, userA), new UserContext(cookieB, userB));
    }

    internal static HttpRequestMessage WithCookie(HttpRequestMessage request, string authCookie)
    {
        request.Headers.TryAddWithoutValidation("Cookie", authCookie);
        return request;
    }

    internal static string NewSentinel(string prefix = "SECRET_SENTINEL") =>
        prefix + "_" + Guid.NewGuid().ToString("N");

    internal static string BuildValidCoverLetterContent(string sentinel)
    {
        var baseContent = sentinel + "_AUTHZ_TEST_";
        return baseContent.Length >= 50 ? baseContent : baseContent + new string('a', 50 - baseContent.Length);
    }

    internal static async Task<string> AssertNoLeakAndGetBodyAsync(HttpResponseMessage response, string sentinel)
    {
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(sentinel);
        return body;
    }

    internal static async Task<(int TotalCount, int ItemCount, string Body)> ReadPagedEnvelopeAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        var root = document.RootElement;
        var totalCount = root.GetProperty("totalCount").GetInt32();
        var itemCount = root.GetProperty("items").GetArrayLength();

        return (totalCount, itemCount, body);
    }

    internal static async Task<Guid> SeedResumeAsync(
        JobNectoApiFactory factory,
        Guid userId,
        string sentinel)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureUserExistsAsync(db, userId);

        var now = DateTime.UtcNow;
        var resume = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = sentinel,
            Skills = ["csharp", "dotnet"],
            WorkLocationType = WorkLocationType.Remote,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Resumes.Add(resume);
        await db.SaveChangesAsync();

        return resume.Id;
    }

    internal static async Task<Guid> SeedEducationAsync(
        JobNectoApiFactory factory,
        Guid userId,
        string sentinel)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureUserExistsAsync(db, userId);

        var now = DateTime.UtcNow;
        var education = new Education
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = sentinel,
            Specialization = "Computer Science",
            Degree = Degree.Bachelor,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Educations.Add(education);
        await db.SaveChangesAsync();

        return education.Id;
    }

    internal static async Task<Guid> SeedCoverLetterTemplateAsync(
        JobNectoApiFactory factory,
        Guid userId,
        string sentinel)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureUserExistsAsync(db, userId);

        var now = DateTime.UtcNow;
        var template = new CoverLetterTemplate
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = sentinel,
            Content = BuildValidCoverLetterContent(sentinel),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.CoverLetterTemplates.Add(template);
        await db.SaveChangesAsync();

        return template.Id;
    }

    internal static async Task<Guid> SeedVacancyAsync(
        JobNectoApiFactory factory,
        Guid userId,
        string sentinel)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureUserExistsAsync(db, userId);

        var now = DateTime.UtcNow;
        var vacancy = new Vacancy
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = sentinel,
            Company = "Contoso",
            Location = Location.Poland,
            WorkLocationType = WorkLocationType.Remote,
            JobSource = new JobSource { Name = "LinkedIn", Url = "https://linkedin.com/jobs" },
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Vacancies.Add(vacancy);
        await db.SaveChangesAsync();

        return vacancy.Id;
    }

    internal static async Task<Guid> SeedCoverLetterAsync(
        JobNectoApiFactory factory,
        Guid userId,
        Guid vacancyId,
        string sentinel)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await EnsureUserExistsAsync(db, userId);

        var now = DateTime.UtcNow;
        var coverLetter = new CoverLetter
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            VacancyId = vacancyId,
            Content = BuildValidCoverLetterContent(sentinel),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.CoverLetters.Add(coverLetter);
        await db.SaveChangesAsync();

        return coverLetter.Id;
    }

    internal static async Task<T> LoadEntityIgnoringFiltersAsync<T>(
        JobNectoApiFactory factory,
        Guid id,
        Func<AppDbContext, IQueryable<T>>? selector = null)
        where T : class
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var query = selector != null
            ? selector(db).IgnoreQueryFilters()
            : db.Set<T>().IgnoreQueryFilters();

        var entity = await query.SingleAsync(x => EF.Property<Guid>(x, nameof(BaseEntity.Id)) == id);
        return entity;
    }

    private static async Task EnsureUserExistsAsync(AppDbContext db, Guid userId)
    {
        var exists = await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == userId);
        if (exists)
        {
            return;
        }

        var now = DateTime.UtcNow;
        db.Users.Add(new User
        {
            Id = userId,
            Login = "seed_" + Guid.NewGuid().ToString("N")[..8],
            Email = Guid.NewGuid().ToString("N")[..8] + "@example.com",
            Password = "seeded-password-hash",
            CreatedAt = now,
            UpdatedAt = now,
        });

        await db.SaveChangesAsync();
    }
}
