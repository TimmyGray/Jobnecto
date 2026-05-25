using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JobNecto.Domain.Entities;

namespace JobNecto.Tests.API.Authorization;

public class VacanciesAuthorizationTests
{
    [Fact]
    public async Task Get_AnotherUsersVacancy_Returns404_AndNoLeak()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var sentinel = AuthorizationTestFixture.NewSentinel("VAC_GET");
        var vacancyId = await AuthorizationTestFixture.SeedVacancyAsync(factory, a.UserId, sentinel);
        var before = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<Vacancy>(factory, vacancyId, db => db.Vacancies);

        var request = AuthorizationTestFixture.WithCookie(new HttpRequestMessage(HttpMethod.Get, $"/api/v1/vacancies/{vacancyId}"), b.AuthCookie);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await AuthorizationTestFixture.AssertNoLeakAndGetBodyAsync(response, sentinel);

        var after = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<Vacancy>(factory, vacancyId, db => db.Vacancies);
        after.UpdatedAt.Should().Be(before.UpdatedAt);
    }

    [Fact]
    public async Task Get_NonExistentVacancy_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (_, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var request = AuthorizationTestFixture.WithCookie(new HttpRequestMessage(HttpMethod.Get, $"/api/v1/vacancies/{Guid.NewGuid()}"), b.AuthCookie);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Filter_UnderUserB_DoesNotLeakUserAVacancies()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var sentinel1 = AuthorizationTestFixture.NewSentinel("VAC_FILTER_1");
        var sentinel2 = AuthorizationTestFixture.NewSentinel("VAC_FILTER_2");
        await AuthorizationTestFixture.SeedVacancyAsync(factory, a.UserId, sentinel1);
        await AuthorizationTestFixture.SeedVacancyAsync(factory, a.UserId, sentinel2);

        var request = AuthorizationTestFixture.WithCookie(
            new HttpRequestMessage(HttpMethod.Post, "/api/v1/vacancies/filter")
            {
                Content = JsonContent.Create(new { }),
            },
            b.AuthCookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var (totalCount, itemCount, body) = await AuthorizationTestFixture.ReadPagedEnvelopeAsync(response);
        totalCount.Should().Be(0);
        itemCount.Should().Be(0);
        body.Should().NotContain(sentinel1);
        body.Should().NotContain(sentinel2);
    }
}
