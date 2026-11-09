using Kairos.Application.Common;
using Kairos.Domain.Common;
using static Kairos.Application.Tests.ReadSideFixture;

namespace Kairos.Application.Tests.Market;

public class MarketServiceTests
{
    private readonly ReadSideFixture fixture = new();
    private readonly CancellationToken none = CancellationToken.None;

    [Fact]
    public async Task Bars_stop_before_the_market_date()
    {
        var bars = await fixture.Market.GetDailyBarsAsync(Winner.Id, 100, none);

        Assert.Equal([TwoDaysAgo, Yesterday], bars.Select(bar => bar.Date));
    }

    [Fact]
    public async Task The_number_of_bars_can_be_limited()
    {
        var bars = await fixture.Market.GetDailyBarsAsync(Winner.Id, 1, none);

        Assert.Equal(Yesterday, Assert.Single(bars).Date);
    }

    [Fact]
    public async Task Bars_of_an_unknown_instrument_are_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Market.GetDailyBarsAsync(Guid.NewGuid(), 10, none));
    }

    [Fact]
    public async Task Without_a_date_the_curve_is_the_live_one()
    {
        var curve = await fixture.Market.GetCurveAsync(null, none);

        Assert.Equal(Today, curve.Date);
        Assert.Equal(2m, curve.Points[0].YieldPercent);
    }

    [Fact]
    public async Task A_past_curve_is_read_from_its_close()
    {
        var curve = await fixture.Market.GetCurveAsync(Yesterday, none);

        Assert.Equal(Yesterday, curve.Date);
        Assert.Equal(3m, curve.Points[0].YieldPercent);
        Assert.Equal(11, curve.Points.Count);
    }

    [Fact]
    public async Task A_curve_from_the_future_is_rejected()
    {
        await Assert.ThrowsAsync<DomainException>(() => fixture.Market.GetCurveAsync(Today.AddDays(1), none));
    }

    [Fact]
    public async Task A_curve_on_a_non_market_day_is_rejected()
    {
        await Assert.ThrowsAsync<DomainException>(() => fixture.Market.GetCurveAsync(new DateOnly(2016, 6, 19), none));
    }

    [Fact]
    public async Task Reference_data_lists_what_a_ticket_needs()
    {
        var data = await fixture.Market.GetReferenceDataAsync(none);

        Assert.Equal(["LOS", "WIN", "UST 10Y"], data.Instruments.Select(item => item.Symbol));
        Assert.Equal(["A Book", "B Book"], data.Books.Select(item => item.Name));
        Assert.Single(data.Traders);
        Assert.Single(data.Counterparties);
    }

    [Fact]
    public async Task A_bond_shows_its_terms_and_whether_it_still_trades()
    {
        var data = await fixture.Market.GetReferenceDataAsync(none);

        var bond = data.Instruments.Single(item => item.Symbol == "UST 10Y");

        Assert.True(bond.IsTradable);
        Assert.Equal(new DateOnly(2024, 1, 15), bond.MaturityDate);
        Assert.Equal(0.02m, bond.CouponRate);
    }
}
