using System.Net;
using System.Text.Json;
using FluentAssertions;
using JobNecto.API;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace JobNecto.Tests.API;

/// <summary>
/// Factory for the OpenAPI schema tests. Mirrors <see cref="CorsFactory"/>: no real
/// database is touched by <c>GET /openapi/v1.json</c>, so a dummy Postgres connection
/// string is enough, and <c>/openapi/v1.json</c> is only mapped in Development.
/// </summary>
public class OpenApiSchemaFactory : WebApplicationFactory<ApiAssemblyMarker>
{
    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Postgres", "Host=localhost;Database=testing;Username=test;Password=test");
        builder.UseSetting("JwtSettings:SecretKey", "ThisIsATestOnlyJwtSecretKeyNotForProduction_1234567890abcdef");
        builder.UseSetting("JwtSettings:Issuer", "JobNecto");
        builder.UseSetting("JwtSettings:Audience", "JobNecto-API");
        builder.UseEnvironment("Development");
    }
}

/// <summary>
/// Verifies Story 2.1's résumé-relevant enums are typed as strings (with real member
/// names) in the generated OpenAPI document, matching the <c>JsonStringEnumConverter</c>
/// already used for actual runtime serialization — not as opaque integers.
/// </summary>
public class OpenApiSchemaTests : IClassFixture<OpenApiSchemaFactory>
{
    private readonly HttpClient _client;

    public OpenApiSchemaTests(OpenApiSchemaFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task WorkLocationType_Schema_IsStringWithExactMembers()
    {
        var schema = await GetSchemaAsync("WorkLocationType");

        AssertStringEnum(schema, ["OnSite", "Remote", "Hybrid"]);
    }

    [Fact]
    public async Task Currency_Schema_IsStringWithExactMembers()
    {
        var schema = await GetSchemaAsync("Currency");

        AssertStringEnum(schema, [
            "USD", "EUR", "GBP", "CAD", "AUD", "CHF", "JPY", "CNY", "INR",
            "RUB", "UAH", "PLN", "SEK", "NOK", "DKK", "NZD", "MXN", "BRL",
        ]);
    }

    [Fact]
    public async Task LanguageLevel_Schema_IsStringWithExactMembers()
    {
        var schema = await GetSchemaAsync("LanguageLevel");

        AssertStringEnum(schema, ["Beginner", "Intermediate", "Advanced", "Native"]);
    }

    [Fact]
    public async Task Language_Schema_IsStringWithExpectedCountAndMembers()
    {
        var schema = await GetSchemaAsync("Language");

        var members = EnumMembers(schema);
        members.Should().HaveCount(38);
        members.Should().Contain(["English", "Ukrainian", "Urdu"]);
    }

    [Fact]
    public async Task Location_Schema_IsStringWithExpectedCountAndMembers()
    {
        var schema = await GetSchemaAsync("Location");

        var members = EnumMembers(schema);
        members.Should().HaveCount(122);
        members.Should().Contain(["Ukraine", "UnitedStates", "Panama"]);
    }

    /// <summary>
    /// <c>Experience</c> is never exposed as an actual enum-typed property anywhere in
    /// the API (only as plain <c>string</c>/<c>string?</c>), so it has no named schema
    /// in the document at all — confirm that stays true rather than silently drifting.
    /// </summary>
    [Fact]
    public async Task Experience_HasNoNamedSchema()
    {
        var document = await FetchDocumentAsync();
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        schemas.TryGetProperty("Experience", out _).Should().BeFalse();
    }

    private async Task<JsonElement> GetSchemaAsync(string schemaName)
    {
        var document = await FetchDocumentAsync();
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        schemas.TryGetProperty(schemaName, out var schema)
            .Should().BeTrue($"'{schemaName}' should be a named schema in the OpenAPI document");

        return schema;
    }

    private async Task<JsonDocument> FetchDocumentAsync()
    {
        var response = await _client.GetAsync("/openapi/v1.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private static void AssertStringEnum(JsonElement schema, string[] expectedMembers) =>
        EnumMembers(schema).Should().BeEquivalentTo(expectedMembers);

    /// <summary>
    /// Reads the schema's <c>enum</c> array, asserting every member is a JSON string
    /// (not a number) — the .NET OpenAPI exporter renders a string-converted enum as
    /// <c>{"enum": ["OnSite", ...]}</c> with no separate <c>"type": "string"</c> field.
    /// </summary>
    private static List<string> EnumMembers(JsonElement schema)
    {
        var values = schema.GetProperty("enum").EnumerateArray().ToList();
        values.Should().OnlyContain(e => e.ValueKind == JsonValueKind.String);
        return values.Select(e => e.GetString()!).ToList();
    }
}
