using System.Net;
using System.Net.Http.Json;
using Kairos.Application.Abstractions;
using Kairos.Application.Market;
using Kairos.Application.Portfolio;
using Kairos.Application.Replay;
using Kairos.Application.Risk;
using Kairos.Domain.ValueObjects;

namespace Kairos.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class ReadAndReplayApiTests(ApiFixture api)
{
    [Fact]
    public async Task Health_endpoints_report_a_working_api()
    {
        Assert.Equal(HttpStatusCode.OK, (await api.Client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.Client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task Swagger_describes_the_api()
    {
        var document = await api.Client.GetStringAsync("/swagger/v1/swagger.json");

        Assert.Contains("/api/trades", document);
        Assert.Contains("/api/replay/load", document);
    }

    [Fact]
    public async Task Reference_data_lists_the_universe_and_fictional_parties()
    {
        var reference = await api.GetAsync<ReferenceDataDto>("/api/market/reference");

        Assert.Equal(40, reference.Instruments.Count(item => item.AssetClass == AssetClass.Equity));
        Assert.Equal(4, reference.Instruments.Count(item => item.AssetClass == AssetClass.Bond));
        Assert.Equal(5, reference.Books.Count);
        Assert.All(reference.Counterparties, item => Assert.StartsWith("Cpty ", item.Name));
    }

    [Fact]
    public async Task The_dashboard_and_summary_agree()
    {
        var dashboard = await api.GetAsync<DashboardDto>("/api/portfolio/dashboard");

        Assert.Equal(ApiFixture.ThirdDay, dashboard.Summary.MarketDate);
        Assert.Equal(5, dashboard.Summary.Books.Count);
        Assert.Equal([ApiFixture.FirstDay, ApiFixture.SecondDay], dashboard.History.Select(point => point.Date));
    }

    [Fact]
    public async Task The_risk_report_has_exposures_and_sensitivities()
    {
        var report = await api.GetAsync<RiskReportDto>("/api/risk");

        Assert.NotEmpty(report.ByAssetClass);
        Assert.NotEmpty(report.BySector);
        Assert.Equal(5, report.Books.Count);
    }

    [Fact]
    public async Task A_parametric_stress_returns_results_by_book()
    {
        var result = await api.PostAsync<StressResultDto>("/api/risk/stress/parametric", new ParametricStressRequest(-10m, -5m, 100m));

        Assert.Equal(5, result.ByBook.Count);
        Assert.Equal(result.TotalEur, result.ByBook.Sum(line => line.Eur), 0);
    }

    [Fact]
    public async Task A_historical_stress_uses_two_market_days()
    {
        var result = await api.PostAsync<StressResultDto>(
            "/api/risk/stress/historical",
            new HistoricalStressRequest(ApiFixture.FirstDay, ApiFixture.SecondDay, "Test"));

        Assert.Equal("Test", result.Scenario);
    }

    [Fact]
    public async Task A_historical_stress_on_a_non_market_day_is_refused()
    {
        var response = await api.Client.PostAsJsonAsync(
            "/api/risk/stress/historical",
            new HistoricalStressRequest(new DateOnly(2016, 6, 19), ApiFixture.SecondDay),
            ApiFixture.Json);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Bars_stop_before_the_replayed_date()
    {
        var reference = await api.GetAsync<ReferenceDataDto>("/api/market/reference");
        var equity = reference.Instruments.First(item => item.AssetClass == AssetClass.Equity);

        var bars = await api.GetAsync<List<DailyBar>>($"/api/market/instruments/{equity.Id}/bars?count=10");

        Assert.Equal([ApiFixture.FirstDay, ApiFixture.SecondDay], bars.Select(bar => bar.Date));
    }

    [Fact]
    public async Task A_curve_can_be_read_for_a_past_date_but_not_a_future_one()
    {
        var past = await api.GetAsync<CurveDto>("/api/market/curve?date=2016-06-20");
        var future = await api.Client.GetAsync("/api/market/curve?date=2016-06-23");

        Assert.Equal(11, past.Points.Count);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, future.StatusCode);
    }

    [Fact]
    public async Task The_replay_can_be_paused_moved_and_resumed()
    {
        await api.PostAsync<ReplayStatusDto>("/api/replay/pause");

        var moved = await api.PostAsync<ReplayStatusDto>("/api/replay/seek", new SeekReplayRequest(900));
        Assert.False(moved.IsPlaying);
        Assert.Equal(900, moved.Minute);

        var faster = await api.PostAsync<ReplayStatusDto>("/api/replay/speed", new SetReplaySpeedRequest(600));
        Assert.Equal(600, faster.Speed);

        Assert.True((await api.PostAsync<ReplayStatusDto>("/api/replay/play")).IsPlaying);
    }

    [Fact]
    public async Task Invalid_replay_commands_are_rejected()
    {
        var speed = await api.Client.PostAsJsonAsync("/api/replay/speed", new SetReplaySpeedRequest(7), ApiFixture.Json);
        var day = await api.Client.PostAsJsonAsync("/api/replay/load", new LoadReplayRequest(ApiFixture.FirstDay), ApiFixture.Json);

        Assert.Equal(HttpStatusCode.BadRequest, speed.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, day.StatusCode);
    }

    [Fact]
    public async Task Remarkable_days_are_listed()
    {
        var days = await api.GetAsync<List<RemarkableDayDto>>("/api/replay/days");

        Assert.Contains(days, day => day.Name == "Brexit");
    }
}
