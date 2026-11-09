using Kairos.Infrastructure.Import;

namespace Kairos.Infrastructure.Tests.Import;

public sealed class CsvMarketDataReaderTests : IDisposable
{
    private const string CurveHeader = "date,US1M,US3M,US6M,US1Y,US2Y,US3Y,US5Y,US7Y,US10Y,US20Y,US30Y";
    private const string StockHeader = "Date,Symbol,Adj Close,Close,High,Low,Open,Volume";
    private const string FxHeader = "Date,Time,BO,BH,BL,BC,BCh,AO,AH,AL,AC,ACh";

    private static readonly DateOnly Monday = new(2016, 6, 20);
    private static readonly DateOnly Tuesday = new(2016, 6, 21);

    private readonly RawDataFolder folder = new();

    public void Dispose() => folder.Dispose();

    [Fact]
    public void Curves_convert_percentages_and_map_columns_to_tenors()
    {
        File.WriteAllLines(folder.TreasuryPath, [CurveHeader, "2016-06-20,1,2,3,4,5,6,7,8,9.5,10,11"]);

        var curves = CsvMarketDataReader.ReadCurves(folder.TreasuryPath);

        Assert.Equal(0.01m, curves[Monday][1]);
        Assert.Equal(0.095m, curves[Monday][120]);
        Assert.Equal(0.11m, curves[Monday][360]);
    }

    [Fact]
    public void Dates_without_a_complete_curve_are_dropped()
    {
        File.WriteAllLines(folder.TreasuryPath,
        [
            CurveHeader,
            "2016-06-20,,,,,,,,,,,",
            "2016-06-21,,2,3,4,5,6,7,8,9,10,11",
            "2016-06-22,1,2,3,4,5,6,7,8,9,10,11",
        ]);

        var curves = CsvMarketDataReader.ReadCurves(folder.TreasuryPath);

        Assert.Equal([new DateOnly(2016, 6, 22)], curves.Keys);
    }

    [Fact]
    public void Equity_bars_read_the_right_columns_and_round_float_noise()
    {
        File.WriteAllLines(folder.StocksPath, [StockHeader, "2016-06-20,AAA,9.1,30.950000762939453,31.5,30.25,30.75,1000"]);

        var bar = Assert.Single(CsvMarketDataReader.ReadEquityBars(folder.StocksPath, new HashSet<string> { "AAA" })[Monday]);

        Assert.Equal("AAA", bar.Ticker);
        Assert.Equal(30.75m, bar.Open);
        Assert.Equal(31.5m, bar.High);
        Assert.Equal(30.25m, bar.Low);
        Assert.Equal(30.95m, bar.Close);
    }

    [Fact]
    public void Other_tickers_and_rows_without_prices_are_skipped()
    {
        File.WriteAllLines(folder.StocksPath,
        [
            StockHeader,
            "2016-06-20,AAA,,,,,,",
            "2016-06-20,BBB,1,2,3,1,2,10",
            "2016-06-21,AAA,1,2,3,1,2,10",
        ]);

        var bars = CsvMarketDataReader.ReadEquityBars(folder.StocksPath, new HashSet<string> { "AAA" });

        Assert.Equal([Tuesday], bars.Keys);
    }

    [Fact]
    public void Fx_days_use_the_closing_bid_and_ask_and_times_without_leading_zero()
    {
        File.WriteAllLines(folder.EurUsdPath,
        [
            FxHeader,
            "2016-06-20,0:46,1.1,1.1,1.1,1.0874700000000002,0,1.2,1.2,1.2,1.08758,0",
            "2016-06-20,18:29,1.1,1.1,1.1,1.1,0,1.2,1.2,1.2,1.1002,0",
        ]);

        var day = Assert.Single(CsvMarketDataReader.ReadEurUsdDays(folder.EurUsdPath, _ => true));

        Assert.Equal(Monday, day.Date);
        Assert.Equal(46, day.Bars[0].Minute);
        Assert.Equal(1.08747m, day.Bars[0].Bid);
        Assert.Equal(1.08758m, day.Bars[0].Ask);
        Assert.Equal(18 * 60 + 29, day.Bars[1].Minute);
    }

    [Fact]
    public void Fx_days_can_be_filtered_and_each_keeps_its_own_bars()
    {
        File.WriteAllLines(folder.EurUsdPath,
        [
            FxHeader,
            "2016-06-19,10:00,1,1,1,1.0,0,1,1,1,1.1,0",
            "2016-06-20,10:00,1,1,1,1.0,0,1,1,1,1.1,0",
            "2016-06-20,10:01,1,1,1,1.0,0,1,1,1,1.1,0",
            "2016-06-21,10:00,1,1,1,1.0,0,1,1,1,1.1,0",
        ]);

        var days = CsvMarketDataReader.ReadEurUsdDays(folder.EurUsdPath, date => date >= Monday).ToList();

        Assert.Equal([Monday, Tuesday], days.Select(day => day.Date));
        Assert.Equal([2, 1], days.Select(day => day.Bars.Count));
    }

    [Fact]
    public void A_repeated_minute_keeps_its_latest_quote()
    {
        File.WriteAllLines(folder.EurUsdPath,
        [
            FxHeader,
            "2016-06-20,10:00,1,1,1,1.0,0,1,1,1,1.1,0",
            "2016-06-20,10:00,1,1,1,1.2,0,1,1,1,1.3,0",
        ]);

        var bar = Assert.Single(Assert.Single(CsvMarketDataReader.ReadEurUsdDays(folder.EurUsdPath, _ => true)).Bars);

        Assert.Equal(1.2m, bar.Bid);
    }
}
