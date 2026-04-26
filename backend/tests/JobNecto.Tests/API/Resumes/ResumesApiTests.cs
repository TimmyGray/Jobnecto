using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JobNecto.Application.Users;
using JobNecto.Application.Resumes;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JobNecto.Tests.API.Resumes;

public class ResumesApiTests
{
    [Fact]
    public async Task Create_ValidResume_Returns201()
    {
        // 1. Setup factory and authenticated client
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        
        // 2. Register a user to get an auth context
        var userCmd = new CreateUserCommand
        {
            LoginName = "resumeuser",
            Email = "resume@example.com",
            Password = "Password123!"
        };
        var userResponse = await client.PostAsJsonAsync("/api/v1/users", userCmd);
        userResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Extract token from cookie for bearer header (simplest for testing)
        var cookie = userResponse.Headers.GetValues("Set-Cookie").First();
        var token = cookie.Split(';')[0].Split('=')[1];
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        // 3. Post a resume
        var resumeCmd = new CreateResumeCommand
        {
            Title = "Fullstack Developer",
            Skills = new[] { "React", "Node.js", "C#" },
            WorkLocationType = "remote",
            Salary = 90000,
            Currency = "EUR"
        };

        var response = await client.PostAsJsonAsync("/api/v1/resumes", resumeCmd);

        // 4. Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        
        var result = await response.Content.ReadFromJsonAsync<ResumeResult>();
        result.Should().NotBeNull();
        result!.Title.Should().Be(resumeCmd.Title);
        result.WorkLocationType.Should().Be(resumeCmd.WorkLocationType);
    }

    [Fact]
    public async Task Create_MissingTitle_Returns201_Because_Logic_Allows_Optional()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        
        // Register & Auth
        var userCmd = new CreateUserCommand { LoginName = "user2", Email = "u2@ex.com", Password = "Password123!" };
        var userResponse = await client.PostAsJsonAsync("/api/v1/users", userCmd);
        var token = userResponse.Headers.GetValues("Set-Cookie").First().Split(';')[0].Split('=')[1];
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        var resumeCmd = new CreateResumeCommand
        {
            Title = "", // Allowed by current validator logic
            Skills = new[] { "C#" },
            WorkLocationType = "remote"
        };

        var response = await client.PostAsJsonAsync("/api/v1/resumes", resumeCmd);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_Unauthorized_Returns401()
    {
        await using var factory = new JobNectoApiFactory();
        var client = factory.CreateClient();

        var resumeCmd = new CreateResumeCommand
        {
            Title = "Dev",
            Skills = new[] { "C#" },
            WorkLocationType = "remote"
        };

        var response = await client.PostAsJsonAsync("/api/v1/resumes", resumeCmd);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
