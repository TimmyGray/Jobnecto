using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JobNecto.Domain.Entities;

namespace JobNecto.Tests.API.Authorization;

public class EducationsAuthorizationTests
{
    [Fact]
    public async Task Get_AnotherUsersEducation_Returns404_AndNoLeak()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var sentinel = AuthorizationTestFixture.NewSentinel("EDU_GET");
        var educationId = await AuthorizationTestFixture.SeedEducationAsync(factory, a.UserId, sentinel);
        var before = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<Education>(factory, educationId, db => db.Educations);

        var request = AuthorizationTestFixture.WithCookie(new HttpRequestMessage(HttpMethod.Get, $"/api/v1/educations/{educationId}"), b.AuthCookie);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await AuthorizationTestFixture.AssertNoLeakAndGetBodyAsync(response, sentinel);

        var after = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<Education>(factory, educationId, db => db.Educations);
        after.UpdatedAt.Should().Be(before.UpdatedAt);
    }

    [Fact]
    public async Task Get_NonExistentEducation_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (_, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var request = AuthorizationTestFixture.WithCookie(new HttpRequestMessage(HttpMethod.Get, $"/api/v1/educations/{Guid.NewGuid()}"), b.AuthCookie);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_AnotherUsersEducation_Returns403_AndNoLeak_AndUnchanged()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var sentinel = AuthorizationTestFixture.NewSentinel("EDU_PATCH");
        var educationId = await AuthorizationTestFixture.SeedEducationAsync(factory, a.UserId, sentinel);
        var before = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<Education>(factory, educationId, db => db.Educations);

        var request = AuthorizationTestFixture.WithCookie(
            new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/educations/{educationId}")
            {
                Content = JsonContent.Create(new
                {
                    title = "patched-title",
                    specialization = "patched-specialization",
                    degree = "master",
                }),
            },
            b.AuthCookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AuthorizationTestFixture.AssertNoLeakAndGetBodyAsync(response, sentinel);

        var after = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<Education>(factory, educationId, db => db.Educations);
        after.Title.Should().Be(before.Title);
        after.Specialization.Should().Be(before.Specialization);
        after.UpdatedAt.Should().Be(before.UpdatedAt);
    }

    [Fact]
    public async Task Patch_NonExistentEducation_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (_, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var request = AuthorizationTestFixture.WithCookie(
            new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/educations/{Guid.NewGuid()}")
            {
                Content = JsonContent.Create(new
                {
                    title = "patched-title",
                    specialization = "patched-specialization",
                    degree = "master",
                }),
            },
            b.AuthCookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_AnotherUsersEducation_Returns403_AndEntityNotSoftDeleted()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var sentinel = AuthorizationTestFixture.NewSentinel("EDU_DELETE");
        var educationId = await AuthorizationTestFixture.SeedEducationAsync(factory, a.UserId, sentinel);

        var request = AuthorizationTestFixture.WithCookie(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/educations/{educationId}"), b.AuthCookie);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await AuthorizationTestFixture.AssertNoLeakAndGetBodyAsync(response, sentinel);

        var entity = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<Education>(factory, educationId, db => db.Educations);
        entity.IsDeleted.Should().BeFalse();
        entity.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NonExistentEducation_Returns404()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (_, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var request = AuthorizationTestFixture.WithCookie(new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/educations/{Guid.NewGuid()}"), b.AuthCookie);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_UnderUserB_DoesNotLeakUserAEducations()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();
        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var sentinel1 = AuthorizationTestFixture.NewSentinel("EDU_LIST_1");
        var sentinel2 = AuthorizationTestFixture.NewSentinel("EDU_LIST_2");
        await AuthorizationTestFixture.SeedEducationAsync(factory, a.UserId, sentinel1);
        await AuthorizationTestFixture.SeedEducationAsync(factory, a.UserId, sentinel2);

        var request = AuthorizationTestFixture.WithCookie(new HttpRequestMessage(HttpMethod.Get, "/api/v1/educations"), b.AuthCookie);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var (totalCount, itemCount, body) = await AuthorizationTestFixture.ReadPagedEnvelopeAsync(response);
        totalCount.Should().Be(0);
        itemCount.Should().Be(0);
        body.Should().NotContain(sentinel1);
        body.Should().NotContain(sentinel2);
    }
}
