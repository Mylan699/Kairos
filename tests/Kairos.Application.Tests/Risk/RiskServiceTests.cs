using FluentValidation;
using Kairos.Application.Risk;
using Kairos.Domain.Common;
using static Kairos.Application.Tests.ReadSideFixture;

namespace Kairos.Application.Tests.Risk;

public class RiskServiceTests
{
    private readonly ReadSideFixture fixture = new();
    private readonly CancellationToken none = CancellationToken.None;

    [Fact]
    public async Task The_report_sums_exposures_and_sensitivities()
    {
        fixture.Buy(Winner, 100m, 90m);

        var report = await fixture.Risk.GetReportAsync(none);

        Assert.Equal(Today, report.MarketDate);
        Assert.Equal(8_800m, Assert.Single(report.ByAssetClass).Eur);
        Assert.Equal("Energy", Assert.Single(report.BySector).Key);
        Assert.Equal(88m, report.EquityDelta);
    }

    [Fact]
    public async Task The_report_gives_each_book_its_own_sensitivities()
    {
        fixture.Buy(Winner, 100m, 90m, BookA);
        fixture.Buy(Winner, 50m, 90m, BookB);

        var report = await fixture.Risk.GetReportAsync(none);

        Assert.Equal([88m, 44m], report.Books.Select(book => book.EquityDelta));
        Assert.Equal(132m, report.EquityDelta);
    }

    [Fact]
    public async Task A_parametric_scenario_converts_percentages_and_reports_by_book()
    {
        fixture.Buy(Winner, 100m, 90m, BookA);
        fixture.Buy(Loser, 100m, 110m, BookB);

        var result = await fixture.Risk.RunParametricAsync(new ParametricStressRequest(-10m, 0m, 0m), none);

        Assert.Equal(-1_600m, result.TotalEur);
        Assert.Equal(["A Book", "B Book"], result.ByBook.Select(line => line.Book));
        Assert.Equal([-880m, -720m], result.ByBook.Select(line => line.Eur));
    }

    [Fact]
    public async Task A_historical_scenario_applies_the_observed_moves_to_todays_book()
    {
        // Between the two closes: the winner rises from 90 to 100, or +11.11 %.
        fixture.Buy(Winner, 100m, 90m);

        var result = await fixture.Risk.RunHistoricalAsync(new HistoricalStressRequest(TwoDaysAgo, Yesterday), none);

        Assert.Equal(977.78m, result.TotalEur);
        Assert.Equal("2016-06-22 → 2016-06-23", result.Scenario);
    }

    [Fact]
    public async Task A_named_historical_scenario_keeps_its_name()
    {
        fixture.Buy(Winner, 100m, 90m);

        var result = await fixture.Risk.RunHistoricalAsync(new HistoricalStressRequest(TwoDaysAgo, Yesterday, "Test"), none);

        Assert.Equal("Test", result.Scenario);
    }

    [Fact]
    public async Task A_historical_scenario_needs_market_days()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            fixture.Risk.RunHistoricalAsync(new HistoricalStressRequest(new DateOnly(2016, 6, 19), Yesterday), none));
    }

    [Fact]
    public async Task A_historical_scenario_needs_dates_in_order()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            fixture.Risk.RunHistoricalAsync(new HistoricalStressRequest(Yesterday, TwoDaysAgo), none));
    }

    [Theory]
    [InlineData(-95, 0, 0)]
    [InlineData(0, 60, 0)]
    [InlineData(0, 0, 900)]
    public async Task Extreme_shocks_are_rejected(double equity, double eurUsd, double rate)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            fixture.Risk.RunParametricAsync(new ParametricStressRequest((decimal)equity, (decimal)eurUsd, (decimal)rate), none));
    }

    [Fact]
    public void Preset_scenarios_have_unique_ids_and_dates_in_order()
    {
        Assert.Equal(HistoricalScenarios.All.Count, HistoricalScenarios.All.Select(item => item.Id).Distinct().Count());
        Assert.All(HistoricalScenarios.All, item => Assert.True(item.From < item.To));
    }
}
