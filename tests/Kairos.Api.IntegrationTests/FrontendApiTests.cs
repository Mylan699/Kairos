using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kairos.Api.IntegrationTests;

/// <summary>The container image puts the built front in wwwroot: the API then serves it too.</summary>
[Collection(ApiCollection.Name)]
public sealed class FrontendApiTests : IAsyncLifetime
{
    private const string Page = "<!doctype html><title>Kairos</title>";

    private readonly string webRoot = Path.Combine(Path.GetTempPath(), "kairos-web-tests-" + Guid.NewGuid());
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.Combine(webRoot, "assets"));
        File.WriteAllText(Path.Combine(webRoot, "index.html"), Page);
        File.WriteAllText(Path.Combine(webRoot, "assets", "app-3f2a.js"), "console.log('kairos');");

        // Same reason as in the fixture: Program reads its settings before the test host can change them.
        Environment.SetEnvironmentVariable("ASPNETCORE_WEBROOT", webRoot);
        factory = new WebApplicationFactory<Program>();
        client = factory.CreateClient();
        Environment.SetEnvironmentVariable("ASPNETCORE_WEBROOT", null);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        client.Dispose();
        await factory.DisposeAsync();
        Directory.Delete(webRoot, recursive: true);
    }

    [Fact]
    public async Task An_application_address_gets_the_page_which_is_always_revalidated()
    {
        var response = await client.GetAsync("/app/blotter");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Page, await response.Content.ReadAsStringAsync());
        Assert.True(response.Headers.CacheControl!.NoCache);
    }

    [Fact]
    public async Task Hashed_assets_are_cached_for_a_year()
    {
        var response = await client.GetAsync("/assets/app-3f2a.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(TimeSpan.FromDays(365), response.Headers.CacheControl!.MaxAge);
    }

    [Fact]
    public async Task The_api_still_answers_in_front_of_the_page()
    {
        var response = await client.GetAsync("/api/replay/days");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task An_unknown_api_address_is_a_404_not_the_page()
    {
        var response = await client.GetAsync("/api/nothing-here");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }
}
