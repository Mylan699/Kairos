using Kairos.Domain.Instruments;
using Kairos.Domain.Rates;
using Kairos.Domain.Trades;
using Kairos.Domain.ValueObjects;
using Kairos.Infrastructure.Persistence;
using Kairos.Infrastructure.Seeding;

namespace Kairos.Infrastructure.Tests.Seeding;

public class SeedTradeGeneratorTests
{
    private static readonly IReadOnlyList<SeedMarketDay> Days = BuildDays();
    private static readonly IReadOnlyList<Instrument> Instruments = BuildInstruments();
    private static readonly IReadOnlyList<Trade> Trades = new SeedTradeGenerator(Instruments).Generate(Days);

    private static List<SeedMarketDay> BuildDays()
    {
        var bars = ReferenceSeed.EquitySectors.Keys.ToDictionary(
            ReferenceSeed.EquityId,
            ticker => new EquityDailyBarRow { InstrumentId = ReferenceSeed.EquityId(ticker), Open = 50m, High = 52m, Low = 48m, Close = 50m });
        var curve = new YieldCurve(Tenor.CurvePoints.Select(tenor => new CurvePoint(tenor, 0.02m)));

        var days = new List<SeedMarketDay>();
        for (var date = new DateOnly(2010, 1, 4); date < new DateOnly(2013, 1, 1); date = date.AddDays(1))
        {
            if (!ValueDateCalculator.IsWeekend(date))
            {
                days.Add(new SeedMarketDay(date, bars, 1.2500m, 1.2502m, curve));
            }
        }

        return days;
    }

    private static List<Instrument> BuildInstruments()
    {
        var marketDays = Days.Select(day => day.Date).ToList();

        return
        [
            .. ReferenceSeed.Equities(),
            ReferenceSeed.EurUsd(),
            .. BondSchedule.Issue(marketDays, (_, _) => 0.02m),
        ];
    }

    [Fact]
    public void Around_three_thousand_trades_are_generated()
    {
        Assert.InRange(Trades.Count, 2_800, 3_100);
    }

    [Fact]
    public void The_same_input_always_gives_the_same_trades()
    {
        var again = new SeedTradeGenerator(Instruments).Generate(Days);

        Assert.Equal(Trades.Select(trade => trade.Id), again.Select(trade => trade.Id));
        Assert.Equal(Trades.Select(trade => trade.Terms), again.Select(trade => trade.Terms));
    }

    [Fact]
    public void Every_trade_is_dated_on_a_market_day()
    {
        var marketDays = Days.Select(day => day.Date).ToHashSet();

        Assert.All(Trades, trade => Assert.Contains(trade.Terms.TradeDate, marketDays));
    }

    [Fact]
    public void Bonds_are_only_traded_while_they_are_alive()
    {
        var byId = Instruments.ToDictionary(item => item.Id);

        Assert.All(Trades, trade => Assert.True(byId[trade.Terms.InstrumentId].IsTradableOn(trade.Terms.TradeDate)));
    }

    [Fact]
    public void Books_are_open_from_the_first_days()
    {
        var firstDays = Days.Take(10).Select(day => day.Date).ToHashSet();
        var openedBooks = Trades.Where(trade => firstDays.Contains(trade.Terms.TradeDate)).Select(trade => trade.Terms.BookId).Distinct();

        Assert.Equal(ReferenceSeed.Books.Count, openedBooks.Count());
    }

    [Fact]
    public void The_long_only_book_never_goes_short()
    {
        var core = Trades
            .Where(trade => trade.IsLive && trade.Terms.BookId == ReferenceSeed.CoreEquities.Id)
            .GroupBy(trade => trade.Terms.InstrumentId);

        foreach (var instrument in core)
        {
            var position = 0m;
            foreach (var trade in instrument)
            {
                position += trade.Terms.Quantity * trade.Terms.Side.Sign();
                Assert.True(position >= 0m);
            }
        }
    }

    [Fact]
    public void Bond_positions_are_closed_before_they_expire()
    {
        var byId = Instruments.ToDictionary(item => item.Id);
        var lastDate = Days[^1].Date;

        var expired = Trades
            .Where(trade => trade.IsLive && byId[trade.Terms.InstrumentId].Bond?.MaturityDate <= lastDate)
            .GroupBy(trade => (trade.Terms.BookId, trade.Terms.InstrumentId));

        Assert.NotEmpty(expired);
        Assert.All(expired, group => Assert.Equal(0m, group.Sum(trade => trade.Terms.Quantity * trade.Terms.Side.Sign())));
    }

    [Fact]
    public void The_blotter_shows_every_status_and_some_amendments()
    {
        Assert.Equal(4, Trades.Select(trade => trade.Status).Distinct().Count());
        Assert.InRange(Trades.Count(trade => trade.Versions.Count > 1), 100, 300);
    }

    [Fact]
    public void A_cancelled_trade_explains_why()
    {
        Assert.All(
            Trades.Where(trade => trade.Status == TradeStatus.Cancelled),
            trade => Assert.False(string.IsNullOrEmpty(trade.AuditTrail[^1].Details)));
    }

    [Fact]
    public void Eur_usd_trades_buy_at_the_ask_and_sell_at_the_bid()
    {
        var fx = Trades.Where(trade => trade.Terms.InstrumentId == ReferenceSeed.EurUsdId).ToList();

        Assert.All(fx, trade => Assert.Equal(trade.Terms.Side == Side.Buy ? 1.2502m : 1.2500m, trade.Terms.Price));
    }

    [Fact]
    public void The_whole_universe_gets_traded()
    {
        Assert.Equal(ReferenceSeed.EquitySectors.Count + 1, Trades
            .Where(trade => Instruments.Single(item => item.Id == trade.Terms.InstrumentId).AssetClass != AssetClass.Bond)
            .Select(trade => trade.Terms.InstrumentId)
            .Distinct()
            .Count());
    }
}
