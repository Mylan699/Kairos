using Kairos.Domain.ValueObjects;
using static Kairos.Application.Tests.ReadSideFixture;

namespace Kairos.Application.Tests.Portfolio;

public class DashboardServiceTests
{
    private readonly ReadSideFixture fixture = new();
    private readonly CancellationToken none = CancellationToken.None;

    [Fact]
    public async Task Contributors_are_ranked_by_their_day_pnl()
    {
        fixture.Buy(Winner, 100m, 90m);
        fixture.Buy(Loser, 100m, 110m);

        var dashboard = await fixture.Dashboard.GetAsync(none);

        var top = Assert.Single(dashboard.TopContributors);
        var bottom = Assert.Single(dashboard.BottomContributors);
        Assert.Equal("WIN", top.Symbol);
        Assert.Equal(800m, top.DayPnl);
        Assert.Equal("LOS", bottom.Symbol);
        Assert.Equal(-800m, bottom.DayPnl);
    }

    [Fact]
    public async Task The_same_instrument_held_in_two_books_is_one_contributor()
    {
        fixture.Buy(Winner, 100m, 90m, BookA);
        fixture.Buy(Winner, 100m, 90m, BookB);

        var dashboard = await fixture.Dashboard.GetAsync(none);

        Assert.Equal(1_600m, Assert.Single(dashboard.TopContributors).DayPnl);
    }

    [Fact]
    public async Task Exposure_is_given_per_asset_class()
    {
        fixture.Buy(Winner, 100m, 90m);
        fixture.Buy(Bond, 1_000_000m, 100m);

        var dashboard = await fixture.Dashboard.GetAsync(none);

        Assert.Equal([AssetClass.Equity, AssetClass.Bond], dashboard.AssetClasses.Select(item => item.AssetClass));
    }

    [Fact]
    public async Task The_history_tracks_the_total_pnl_at_each_close()
    {
        fixture.Buy(Winner, 100m, 90m);

        var dashboard = await fixture.Dashboard.GetAsync(none);

        Assert.Equal([TwoDaysAgo, Yesterday], dashboard.History.Select(point => point.Date));
        Assert.Equal([0m, 800m], dashboard.History.Select(point => point.Total));
    }

    [Fact]
    public async Task An_empty_desk_gives_an_empty_dashboard()
    {
        var dashboard = await fixture.Dashboard.GetAsync(none);

        Assert.Empty(dashboard.TopContributors);
        Assert.Empty(dashboard.AssetClasses);
        Assert.Equal(0m, dashboard.Summary.Total.Total);
    }
}
