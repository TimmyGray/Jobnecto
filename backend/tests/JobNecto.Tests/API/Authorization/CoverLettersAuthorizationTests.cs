using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JobNecto.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobNecto.Tests.API.Authorization;

public class CoverLettersAuthorizationTests
{
    [Fact]
    public async Task Get_AnotherUsersCoverLetter_Returns404_AndNoLeak()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var vacancySentinel = AuthorizationTestFixture.NewSentinel("CL_GET_VAC");
        var coverLetterSentinel = AuthorizationTestFixture.NewSentinel("CL_GET");
        var vacancyId = await AuthorizationTestFixture.SeedVacancyAsync(factory, a.UserId, vacancySentinel);
        var coverLetterId = await AuthorizationTestFixture.SeedCoverLetterAsync(factory, a.UserId, vacancyId, coverLetterSentinel);
        var before = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<CoverLetter>(factory, coverLetterId, db => db.CoverLetters);

        var request = AuthorizationTestFixture.WithCookie(new HttpRequestMessage(HttpMethod.Get, $"/api/v1/cover-letters/{coverLetterId}"), b.AuthCookie);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await AuthorizationTestFixture.AssertNoLeakAndGetBodyAsync(response, coverLetterSentinel);

        var after = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<CoverLetter>(factory, coverLetterId, db => db.CoverLetters);
        after.UpdatedAt.Should().Be(before.UpdatedAt);
    }

    [Fact]
    public async Task Get_NonExistentCoverLetter_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (_, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var request = AuthorizationTestFixture.WithCookie(new HttpRequestMessage(HttpMethod.Get, $"/api/v1/cover-letters/{Guid.NewGuid()}"), b.AuthCookie);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_AnotherUsersCoverLetter_Returns403_AndNoLeak_AndUnchanged()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var vacancySentinel = AuthorizationTestFixture.NewSentinel("CL_PATCH_VAC");
        var coverLetterSentinel = AuthorizationTestFixture.NewSentinel("CL_PATCH");
        var vacancyId = await AuthorizationTestFixture.SeedVacancyAsync(factory, a.UserId, vacancySentinel);
        var coverLetterId = await AuthorizationTestFixture.SeedCoverLetterAsync(factory, a.UserId, vacancyId, coverLetterSentinel);
        var before = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<CoverLetter>(factory, coverLetterId, db => db.CoverLetters);

        var request = AuthorizationTestFixture.WithCookie(
            new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/cover-letters/{coverLetterId}")
            {
                Content = JsonContent.Create(new
                {
                    content = AuthorizationTestFixture.BuildValidCoverLetterContent("patched"),
                }),
            },
            b.AuthCookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AuthorizationTestFixture.AssertNoLeakAndGetBodyAsync(response, coverLetterSentinel);

        var after = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<CoverLetter>(factory, coverLetterId, db => db.CoverLetters);
        after.Content.Should().Be(before.Content);
        after.UpdatedAt.Should().Be(before.UpdatedAt);
    }

    [Fact]
    public async Task Patch_NonExistentCoverLetter_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (_, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var request = AuthorizationTestFixture.WithCookie(
            new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/cover-letters/{Guid.NewGuid()}")
            {
                Content = JsonContent.Create(new
                {
                    content = AuthorizationTestFixture.BuildValidCoverLetterContent("patched"),
                }),
            },
            b.AuthCookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AnotherUsersCoverLetter_Returns403_AndEntityNotSoftDeleted()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var vacancySentinel = AuthorizationTestFixture.NewSentinel("CL_DELETE_VAC");
        var coverLetterSentinel = AuthorizationTestFixture.NewSentinel("CL_DELETE");
        var vacancyId = await AuthorizationTestFixture.SeedVacancyAsync(factory, a.UserId, vacancySentinel);
        var coverLetterId = await AuthorizationTestFixture.SeedCoverLetterAsync(factory, a.UserId, vacancyId, coverLetterSentinel);

        var request = AuthorizationTestFixture.WithCookie(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/cover-letters/{coverLetterId}"), b.AuthCookie);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AuthorizationTestFixture.AssertNoLeakAndGetBodyAsync(response, coverLetterSentinel);

        var entity = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<CoverLetter>(factory, coverLetterId, db => db.CoverLetters);
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NonExistentCoverLetter_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (_, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var request = AuthorizationTestFixture.WithCookie(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/cover-letters/{Guid.NewGuid()}"), b.AuthCookie);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_UnderUserB_DoesNotLeakUserACoverLetters()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var vacancySentinel1 = AuthorizationTestFixture.NewSentinel("CL_LIST_V1");
        var vacancySentinel2 = AuthorizationTestFixture.NewSentinel("CL_LIST_V2");
        var letterSentinel1 = AuthorizationTestFixture.NewSentinel("CL_LIST_1");
        var letterSentinel2 = AuthorizationTestFixture.NewSentinel("CL_LIST_2");

        var vacancy1 = await AuthorizationTestFixture.SeedVacancyAsync(factory, a.UserId, vacancySentinel1);
        var vacancy2 = await AuthorizationTestFixture.SeedVacancyAsync(factory, a.UserId, vacancySentinel2);
        await AuthorizationTestFixture.SeedCoverLetterAsync(factory, a.UserId, vacancy1, letterSentinel1);
        await AuthorizationTestFixture.SeedCoverLetterAsync(factory, a.UserId, vacancy2, letterSentinel2);

        var request = AuthorizationTestFixture.WithCookie(new HttpRequestMessage(HttpMethod.Get, "/api/v1/cover-letters"), b.AuthCookie);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var (totalCount, itemCount, body) = await AuthorizationTestFixture.ReadPagedEnvelopeAsync(response);
        totalCount.Should().Be(0);
        itemCount.Should().Be(0);
        body.Should().NotContain(letterSentinel1);
        body.Should().NotContain(letterSentinel2);
    }

    [Fact]
    public async Task Create_WithUserAVacancyId_AsUserB_Returns404_AndNoLeak()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var vacancySentinel = AuthorizationTestFixture.NewSentinel("CL_CREATE_FOREIGN_V");
        var foreignVacancyId = await AuthorizationTestFixture.SeedVacancyAsync(factory, a.UserId, vacancySentinel);

        var request = AuthorizationTestFixture.WithCookie(
            new HttpRequestMessage(HttpMethod.Post, "/api/v1/cover-letters")
            {
                Content = JsonContent.Create(new
                {
                    vacancyId = foreignVacancyId,
                    content = AuthorizationTestFixture.BuildValidCoverLetterContent("foreign-vacancy"),
                }),
            },
            b.AuthCookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await AuthorizationTestFixture.AssertNoLeakAndGetBodyAsync(response, vacancySentinel);

        var bLetters = await CountUserCoverLettersAsync(factory, b.UserId);
        bLetters.Should().Be(0);
    }

    private static async Task<int> CountUserCoverLettersAsync(JobNectoApiFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobNecto.Infrastructure.Persistance.AppDbContext>();
        return await db.CoverLetters.IgnoreQueryFilters().CountAsync(x => x.UserId == userId);
    }
}
