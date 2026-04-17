using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using JobNecto.API;
using JobNecto.Application.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace JobNecto.Tests.API;

public class TestEndpointsStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            next(app); // Sets up the pipeline from Program.cs

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/test-validation", () =>
                {
                    throw new ValidationException(new[] {
                        new ValidationFailure("FieldName", "Mock error message.")
                    });
                });

                endpoints.MapGet("/test-notfound", () =>
                {
                    throw new NotFoundException("This is a not found exception.");
                });

                endpoints.MapGet("/test-forbidden", () =>
                {
                    throw new ForbiddenException();
                });

                endpoints.MapGet("/test-unauthorized", () =>
                {
                    throw new UnauthorizedException();
                });

                endpoints.MapGet("/test-generic", () =>
                {
                    throw new InvalidOperationException("Generic failure");
                });
            });
        };
    }
}

public class ExceptionHandlingFactory : WebApplicationFactory<ApiAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddTransient<IStartupFilter, TestEndpointsStartupFilter>();
        });
        
        // Use UseSetting to override the connection string at the builder level
        // This takes precedence over appsettings.json
        builder.UseSetting("ConnectionStrings:Postgres", "Host=localhost;Database=testing;Username=test;Password=test");
        builder.UseEnvironment("Production");
    }
}

public class ExceptionHandlingTests : IClassFixture<ExceptionHandlingFactory>
{
    private readonly HttpClient _client;

    public ExceptionHandlingTests(ExceptionHandlingFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_TestValidation_Returns400WithErrorsDictionary()
    {
        var response = await _client.GetAsync("/test-validation");
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        var document = JsonNode.Parse(json);

        document.Should().NotBeNull();
        document!["status"]!.GetValue<int>().Should().Be(400);
        document["title"]!.GetValue<string>().Should().Be("Validation failed");
        document["traceId"]!.GetValue<string>().Should().NotBeNullOrEmpty();

        var errors = document["errors"];
        errors.Should().NotBeNull();
        errors!["FieldName"]!.AsArray().Count.Should().Be(1);
        errors!["FieldName"]![0]!.GetValue<string>().Should().Be("Mock error message.");
    }

    [Fact]
    public async Task Get_TestNotFound_Returns404()
    {
        var response = await _client.GetAsync("/test-notfound");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        var document = JsonNode.Parse(json);

        document!["status"]!.GetValue<int>().Should().Be(404);
        document["traceId"]!.GetValue<string>().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_TestForbidden_Returns403()
    {
        var response = await _client.GetAsync("/test-forbidden");
        
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        var document = JsonNode.Parse(json);

        document!["status"]!.GetValue<int>().Should().Be(403);
        document["traceId"]!.GetValue<string>().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_TestUnauthorized_Returns401()
    {
        var response = await _client.GetAsync("/test-unauthorized");
        
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        var document = JsonNode.Parse(json);

        document!["status"]!.GetValue<int>().Should().Be(401);
        document["traceId"]!.GetValue<string>().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_TestGeneric_Returns500AndNoDetailsInProduction()
    {
        var response = await _client.GetAsync("/test-generic");
        
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        var document = JsonNode.Parse(json);

        document!["status"]!.GetValue<int>().Should().Be(500);
        document["traceId"]!.GetValue<string>().Should().NotBeNullOrEmpty();
        document["exception"].Should().BeNull("stack trace should not be leaked in Production");
    }
}
