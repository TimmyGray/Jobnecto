using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using JobNecto.Application.Users;
using JobNecto.Domain.Entities;
using JobNecto.Infrastructure.Persistance;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobNecto.Tests.API.Authorization;

/// <summary>
/// JWT-bound endpoints — there is no path-supplied or body-supplied user id; tests assert the binding cannot be subverted.
/// </summary>
public class UsersMeAuthorizationTests
{
    private static readonly byte[] PngAvatarBytes =
    [
        0x89, 0x50, 0x4E, 0x47,
        0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D,
    ];

    [Fact]
    public async Task Patch_UsersMe_WithBodyImpersonationAttempt_OnlyMutatesCaller()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);
        var about = "about_" + Guid.NewGuid().ToString("N");

        var userBBefore = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<User>(factory, b.UserId, db => db.Users);
        var userBEmailBefore = userBBefore.Email;
        var userBAboutBefore = userBBefore.AboutMe;
        var userBUpdatedAtBefore = userBBefore.UpdatedAt;

        var request = AuthorizationTestFixture.WithCookie(
            new HttpRequestMessage(HttpMethod.Patch, "/api/v1/users/me")
            {
                Content = JsonContent.Create(new
                {
                    about,
                    userId = b.UserId,
                    id = b.UserId,
                    targetUserId = b.UserId,
                }),
            },
            a.AuthCookie);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<GetCurrentUserResult>(AuthorizationTestFixture.JsonOptions);
        profile.Should().NotBeNull();
        profile!.Id.Should().Be(a.UserId);
        profile.About.Should().Be(about);

        var userBAfter = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<User>(factory, b.UserId, db => db.Users);
        userBAfter.Email.Should().Be(userBEmailBefore);
        userBAfter.AboutMe.Should().Be(userBAboutBefore);
        userBAfter.UpdatedAt.Should().Be(userBUpdatedAtBefore);
    }

    [Fact]
    public async Task Patch_UsersMe_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.PatchAsJsonAsync("/api/v1/users/me", new { about = "unauthorized" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostAvatar_UsersMe_OnlyMutatesCaller()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var userBBefore = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<User>(factory, b.UserId, db => db.Users);
        var userBAvatarBefore = userBBefore.Avatar;
        var userBUpdatedAtBefore = userBBefore.UpdatedAt;

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(PngAvatarBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "avatar", "avatar.png");

        using var request = AuthorizationTestFixture.WithCookie(
            new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/avatar")
            {
                Content = content,
            },
            a.AuthCookie);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var caller = await response.Content.ReadFromJsonAsync<GetCurrentUserResult>(AuthorizationTestFixture.JsonOptions);
        caller.Should().NotBeNull();
        caller!.Id.Should().Be(a.UserId);
        caller.Avatar.Should().NotBeNullOrWhiteSpace();

        var userBAfter = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<User>(factory, b.UserId, db => db.Users);
        userBAfter.Avatar.Should().Be(userBAvatarBefore);
        userBAfter.UpdatedAt.Should().Be(userBUpdatedAtBefore);
    }

    [Fact]
    public async Task PutAvatar_UsersMe_OnlyMutatesCaller()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        var userBBefore = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<User>(factory, b.UserId, db => db.Users);
        var userBAvatarBefore = userBBefore.Avatar;
        var userBUpdatedAtBefore = userBBefore.UpdatedAt;

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(PngAvatarBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "avatar", "avatar.png");

        using var request = AuthorizationTestFixture.WithCookie(
            new HttpRequestMessage(HttpMethod.Put, "/api/v1/users/me/avatar")
            {
                Content = content,
            },
            a.AuthCookie);

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var caller = await response.Content.ReadFromJsonAsync<GetCurrentUserResult>(AuthorizationTestFixture.JsonOptions);
        caller.Should().NotBeNull();
        caller!.Id.Should().Be(a.UserId);
        caller.Avatar.Should().NotBeNullOrWhiteSpace();

        var userBAfter = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<User>(factory, b.UserId, db => db.Users);
        userBAfter.Avatar.Should().Be(userBAvatarBefore);
        userBAfter.UpdatedAt.Should().Be(userBUpdatedAtBefore);
    }

    [Fact]
    public async Task DeleteAvatar_UsersMe_OnlyMutatesCaller()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        var (a, b) = await AuthorizationTestFixture.CreateTwoUsersAsync(client);

        using (var uploadContent = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(PngAvatarBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            uploadContent.Add(fileContent, "avatar", "avatar.png");

            using var uploadRequest = AuthorizationTestFixture.WithCookie(
                new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/avatar")
                {
                    Content = uploadContent,
                },
                a.AuthCookie);

            var uploadResponse = await client.SendAsync(uploadRequest);
            uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var userBBefore = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<User>(factory, b.UserId, db => db.Users);
        var userBAvatarBefore = userBBefore.Avatar;
        var userBUpdatedAtBefore = userBBefore.UpdatedAt;

        using var deleteRequest = AuthorizationTestFixture.WithCookie(
            new HttpRequestMessage(HttpMethod.Delete, "/api/v1/users/me/avatar"),
            a.AuthCookie);

        var deleteResponse = await client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var caller = await deleteResponse.Content.ReadFromJsonAsync<GetCurrentUserResult>(AuthorizationTestFixture.JsonOptions);
        caller.Should().NotBeNull();
        caller!.Id.Should().Be(a.UserId);
        caller.Avatar.Should().BeNull();

        var userBAfter = await AuthorizationTestFixture.LoadEntityIgnoringFiltersAsync<User>(factory, b.UserId, db => db.Users);
        userBAfter.Avatar.Should().Be(userBAvatarBefore);
        userBAfter.UpdatedAt.Should().Be(userBUpdatedAtBefore);
    }

    [Fact]
    public async Task PostAvatar_UsersMe_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(PngAvatarBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "avatar", "avatar.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/users/me/avatar")
        {
            Content = content,
        };

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutAvatar_UsersMe_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(PngAvatarBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "avatar", "avatar.png");

        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/users/me/avatar")
        {
            Content = content,
        };

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAvatar_UsersMe_WithoutToken_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync("/api/v1/users/me/avatar");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
