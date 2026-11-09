using Kairos.Application.Abstractions;
using Kairos.Application.Portfolio;
using Kairos.Application.Replay;
using Kairos.Domain.Fx;
using Kairos.Domain.Instruments;
using Kairos.Domain.Market;
using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Tests.Replay;

public class MarketTickBuilderTests
{
    private static readonly DateOnly Yesterday = new(2016, 6, 23);
    private static readonly DateOnly Today = new(2016, 6, 24);
    private static readonly Instrument Equity = Instrument.Equity(Guid.NewGuid(), "AAA", "Energy");
    private static readonly Instrument EurUsd = Instrument.EurUsd(Guid.NewGuid());
    private static readonly Instrument Bond = Instrument.TreasuryBond(
        Guid.NewGuid(), "UST 10Y 2014", TreasuryBond.Issue(Tenor.TenYears, new DateOnly(2014, 1, 15), 0.02m));
    private static readonly Instrument IssuedToday = Instrument.TreasuryBond(
        Guid.NewGuid(), "UST 2Y 2016", TreasuryBond.Issue(Tenor.TwoYears, Today, 0.02m));
    private static readonly Instrument Redeemed = Instrument.TreasuryBond(
        Guid.NewGuid(), "UST 2Y 2012", TreasuryBond.Issue(Tenor.TwoYears, new DateOnly(2012, 1, 16), 0.02m));

    private static readonly ReplayStatusDto Status = new(Today, 900, 120, true, 810, 1200);
    private static readonly PortfolioSummaryDto Summary = new(Today, [], new PnlFigures(0m, 0m, 0m, 0m, 0m, 0m));

    private static MarketSnapshot Snapshot(DateOnly date, decimal price, decimal bid, decimal ask, decimal yield) => new(
        date,
        new Dictionary<Guid, decimal> { [Equity.Id] = price },
        new EurUsdQuote(bid, ask),
        new YieldCurve(Tenor.CurvePoints.Select(tenor => new CurvePoint(tenor, yield))));

    private static MarketTickDto Build()
    {
        var catalog = new ReferenceCatalog([Equity, EurUsd, Bond, IssuedToday, Redeemed], [], [], []);

        return MarketTickBuilder.Build(
            Status,
            Snapshot(Today, 110m, 1.1000m, 1.1002m, 0.03m),
            Snapshot(Yesterday, 100m, 1.2000m, 1.2002m, 0.02m),
            catalog,
            Summary);
    }

    private static QuoteDto Quote(Instrument instrument) => Build().Quotes.Single(quote => quote.InstrumentId == instrument.Id);

    [Fact]
    public void An_equity_quote_has_a_one_cent_spread_and_its_daily_change()
    {
        var quote = Quote(Equity);

        Assert.Equal(109.99m, quote.Bid);
        Assert.Equal(110.01m, quote.Ask);
        Assert.Equal(10m, quote.ChangePercent);
        Assert.Null(quote.YieldPercent);
    }

    [Fact]
    public void Eur_usd_keeps_its_real_bid_and_ask()
    {
        var quote = Quote(EurUsd);

        Assert.Equal(1.1000m, quote.Bid);
        Assert.Equal(1.1002m, quote.Ask);
        Assert.Equal(1.1001m, quote.Last);
        Assert.Equal(-8.33m, quote.ChangePercent);
    }

    [Fact]
    public void A_bond_shows_its_yield_and_loses_value_when_rates_rise()
    {
        var quote = Quote(Bond);

        Assert.Equal(3m, quote.YieldPercent);
        Assert.True(quote.ChangePercent < 0m);
    }

    [Fact]
    public void A_bond_issued_today_shows_no_change()
    {
        Assert.Equal(0m, Quote(IssuedToday).ChangePercent);
    }

    [Fact]
    public void A_redeemed_bond_is_not_quoted()
    {
        Assert.DoesNotContain(Build().Quotes, quote => quote.InstrumentId == Redeemed.Id);
    }

    [Fact]
    public void The_curve_is_sent_in_percent_for_every_maturity()
    {
        var curve = Build().Curve;

        Assert.Equal(11, curve.Points.Count);
        Assert.All(curve.Points, point => Assert.Equal(3m, point.YieldPercent));
    }
}
