using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;

namespace Kairos.Api.IntegrationTests;

/// <summary>
/// Runs the whole API on a real SQL Server, through its normal startup: migrations,
/// import of small raw files, reference data, seed trades and the replay engine.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public static readonly DateOnly FirstDay = new(2016, 6, 20);
    public static readonly DateOnly SecondDay = new(2016, 6, 21);
    public static readonly DateOnly ThirdDay = new(2016, 6, 22);

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly MsSqlContainer container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private readonly string rawPath = Path.Combine(Path.GetTempPath(), "kairos-api-tests-" + Guid.NewGuid());

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        RawFiles.Write(rawPath, FirstDay, SecondDay, ThirdDay);
        await container.StartAsync();

        // Program reads its configuration before the test host can inject settings,
        // so environment variables are the reliable way to reach it.
        Environment.SetEnvironmentVariable("ConnectionStrings__Kairos", container.GetConnectionString());
        Environment.SetEnvironmentVariable("MarketData__RawPath", rawPath);
        Environment.SetEnvironmentVariable("Replay__DefaultDate", ThirdDay.ToString("yyyy-MM-dd"));
        Environment.SetEnvironmentVariable("Replay__TickMilliseconds", "50");
        Environment.SetEnvironmentVariable("Replay__IdleResetMinutes", "0");

        Factory = new WebApplicationFactory<Program>();
        Client = Factory.CreateClient();

        await WaitForMarketAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        await container.DisposeAsync();
        Directory.Delete(rawPath, recursive: true);
    }

    public async Task<T> GetAsync<T>(string path)
    {
        var response = await Client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public async Task<T> PostAsync<T>(string path, object? body = null)
    {
        var response = await Client.PostAsJsonAsync(path, body ?? new { }, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private async Task WaitForMarketAsync()
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var response = await Client.GetAsync("/api/replay");
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("The replay never started.");
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "API";
}
