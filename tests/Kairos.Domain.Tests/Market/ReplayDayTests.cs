using Kairos.Domain.Fx;
using Kairos.Domain.Market;
using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.Market;

public class ReplayDayTests
{
    private const int SummerOpen = 13 * 60 + 30;

    private static readonly DateOnly Date = new(2016, 6, 24);
    private static readonly Guid Equity = Guid.NewGuid();

    private static YieldCurve FlatCurve(decimal yield) =>
        new(Tenor.CurvePoints.Select(tenor => new CurvePoint(tenor, yield)));

    private static ReplayDay Day()
    {
        var path = Enumerable.Range(0, 391).Select(minute => 100m + minute).ToArray();
        var previous = new MarketSnapshot(
            Date.AddDays(-1),
            new Dictionary<Guid, decimal> { [Equity] = 90m },
            new EurUsdQuote(1.1000m, 1.1002m),
            FlatCurve(0.02m));

        FxTick[] ticks =
        [
            new(60, new EurUsdQuote(1.1100m, 1.1102m)),
            new(120, new EurUsdQuote(1.1200m, 1.1202m)),
        ];

        return new ReplayDay(Date, previous, new Dictionary<Guid, decimal[]> { [Equity] = path }, ticks, FlatCurve(0.03m));
    }

    [Fact]
    public void Before_the_open_an_equity_stays_at_the_previous_close()
    {
        Assert.Equal(90m, Day().SnapshotAt(SummerOpen - 1).EquityPrices[Equity]);
    }

    [Fact]
    public void During_the_session_an_equity_follows_its_path()
    {
        Assert.Equal(100m, Day().SnapshotAt(SummerOpen).EquityPrices[Equity]);
        Assert.Equal(160m, Day().SnapshotAt(SummerOpen + 60).EquityPrices[Equity]);
    }

    [Fact]
    public void After_the_close_an_equity_stays_at_the_close()
    {
        Assert.Equal(490m, Day().SnapshotAt(SummerOpen + 391).EquityPrices[Equity]);
    }

    [Fact]
    public void Eur_usd_uses_the_last_real_quote_seen()
    {
        Assert.Equal(1.1100m, Day().SnapshotAt(60).EurUsd.Bid);
        Assert.Equal(1.1100m, Day().SnapshotAt(119).EurUsd.Bid);
        Assert.Equal(1.1200m, Day().SnapshotAt(1_439).EurUsd.Bid);
    }

    [Fact]
    public void Before_the_first_tick_eur_usd_stays_at_the_previous_close()
    {
        Assert.Equal(1.1000m, Day().SnapshotAt(59).EurUsd.Bid);
    }

    [Fact]
    public void The_curve_moves_progressively_from_yesterday_to_today()
    {
        var day = Day();

        Assert.Equal(0.02m, day.SnapshotAt(0).Curve.YieldAt(10m), 8);
        Assert.Equal(0.025m, day.SnapshotAt(720).Curve.YieldAt(10m), 8);
        Assert.Equal(0.03m, day.SnapshotAt(ReplayDay.MinutesPerDay).Curve.YieldAt(10m), 8);
    }

    [Fact]
    public void A_snapshot_carries_the_replayed_date()
    {
        Assert.Equal(Date, Day().SnapshotAt(0).Date);
    }

    [Fact]
    public void The_session_opens_at_half_past_one_utc_in_summer()
    {
        Assert.Equal(new UsEquitySession(SummerOpen, SummerOpen + 390), UsEquitySession.For(Date));
    }

    [Fact]
    public void The_session_opens_at_half_past_two_utc_in_winter()
    {
        Assert.Equal(14 * 60 + 30, UsEquitySession.For(new DateOnly(2016, 1, 15)).OpenMinute);
    }
}
