using System.Net;
using System.Net.Http.Json;
using Kairos.Application.Replay;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Kairos.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class SecurityApiTests(ApiFixture api) : IAsyncDisposable
{
    private WebApplicationFactory<Program>? limitedFactory;

    public async ValueTask DisposeAsync()
    {
        if (limitedFactory is not null)
        {
            await limitedFactory.DisposeAsync();
        }
    }

    /// <summary>Starts a second host of the API with tight limits on the same database.</summary>
    private HttpClient ClientWithLimits(int reads, int writes)
    {
        Environment.SetEnvironmentVariable("RateLimiting__ReadPermitsPerMinute", reads.ToString());
        Environment.SetEnvironmentVariable("RateLimiting__WritePermitsPerMinute", writes.ToString());

        limitedFactory = new WebApplicationFactory<Program>();
        var client = limitedFactory.CreateClient();

        Environment.SetEnvironmentVariable("RateLimiting__ReadPermitsPerMinute", ApiFixture.GenerousLimit);
        Environment.SetEnvironmentVariable("RateLimiting__WritePermitsPerMinute", ApiFixture.GenerousLimit);

        return client;
    }

    [Fact]
    public async Task Too_many_reads_get_a_429_problem_with_a_retry_delay()
    {
        var client = ClientWithLimits(reads: 2, writes: 100);

        await client.GetAsync("/api/replay/days");
        await client.GetAsync("/api/replay/days");
        var rejected = await client.GetAsync("/api/replay/days");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType!.MediaType);
        Assert.NotNull(rejected.Headers.RetryAfter);
    }

    [Fact]
    public async Task Writes_have_a_tighter_limit_than_reads()
    {
        var client = ClientWithLimits(reads: 100, writes: 1);

        // An invalid command is answered without waiting for this host's replay to start.
        var invalid = new SetReplaySpeedRequest(7);
        var first = await client.PostAsJsonAsync("/api/replay/speed", invalid, ApiFixture.Json);
        var second = await client.PostAsJsonAsync("/api/replay/speed", invalid, ApiFixture.Json);
        var read = await client.GetAsync("/api/replay/days");

        Assert.Equal(HttpStatusCode.BadRequest, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }

    [Fact]
    public async Task Each_client_address_has_its_own_quota()
    {
        var client = ClientWithLimits(reads: 1, writes: 100);

        async Task<HttpStatusCode> GetFromAsync(string address)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/replay/days");
            request.Headers.Add("X-Forwarded-For", address);
            return (await client.SendAsync(request)).StatusCode;
        }

        Assert.Equal(HttpStatusCode.OK, await GetFromAsync("203.0.113.1"));
        Assert.Equal(HttpStatusCode.TooManyRequests, await GetFromAsync("203.0.113.1"));
        Assert.Equal(HttpStatusCode.OK, await GetFromAsync("203.0.113.2"));
    }

    [Fact]
    public async Task Health_checks_are_never_limited()
    {
        var client = ClientWithLimits(reads: 1, writes: 1);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        }
    }

    [Fact]
    public async Task The_allowed_origin_passes_the_cors_preflight()
    {
        var response = await PreflightAsync(ApiFixture.AllowedOrigin);

        Assert.Equal(ApiFixture.AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Another_origin_is_refused_by_cors()
    {
        var response = await PreflightAsync("https://elsewhere.example");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Responses_carry_the_security_headers()
    {
        var response = await api.Client.GetAsync("/api/replay/days");

        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.Equal("DENY", Assert.Single(response.Headers.GetValues("X-Frame-Options")));
    }

    [Fact]
    public async Task Replay_commands_still_work_within_the_limits()
    {
        var response = await api.Client.PostAsJsonAsync("/api/replay/seek", new SeekReplayRequest(800), ApiFixture.Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<HttpResponseMessage> PreflightAsync(string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/trades");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        return await api.Client.SendAsync(request);
    }
}
