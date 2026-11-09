using System.Globalization;
using Kairos.Domain.ValueObjects;

namespace Kairos.Infrastructure.Import;

internal sealed record ParsedEquityBar(string Ticker, decimal Open, decimal High, decimal Low, decimal Close);

internal readonly record struct ParsedFxBar(short Minute, decimal Bid, decimal Ask);

internal sealed record ParsedFxDay(DateOnly Date, IReadOnlyList<ParsedFxBar> Bars);

/// <summary>Reads the Kaggle files. None of the columns used contain quoted commas, so a plain split is enough.</summary>
internal static class CsvMarketDataReader
{
    private const int EquityDecimals = 4;
    private const int FxDecimals = 5;
    private const decimal PercentToFraction = 0.01m;

    /// <summary>Returns the yields by date and tenor months, keeping only the dates with a complete curve.</summary>
    public static Dictionary<DateOnly, Dictionary<int, decimal>> ReadCurves(string path)
    {
        using var lines = File.ReadLines(path).GetEnumerator();
        lines.MoveNext();

        // The columns are named US1M, US3M... US30Y: the end of the name is the tenor label.
        var tenorMonths = lines.Current.Split(',').Skip(1).Select(name => Tenor.Parse(name[2..]).Months).ToArray();
        var curves = new Dictionary<DateOnly, Dictionary<int, decimal>>();

        while (lines.MoveNext())
        {
            var fields = lines.Current.Split(',');
            var yields = fields.Skip(1).ToArray();

            if (yields.Any(string.IsNullOrEmpty))
            {
                continue;
            }

            var curve = new Dictionary<int, decimal>();
            for (var index = 0; index < tenorMonths.Length; index++)
            {
                curve[tenorMonths[index]] = ParseDecimal(yields[index]) * PercentToFraction;
            }

            curves[DateOnly.ParseExact(fields[0], "yyyy-MM-dd", CultureInfo.InvariantCulture)] = curve;
        }

        return curves;
    }

    /// <summary>Returns the bars of the requested tickers by date. Rows without prices are skipped.</summary>
    public static Dictionary<DateOnly, List<ParsedEquityBar>> ReadEquityBars(string path, IReadOnlySet<string> tickers)
    {
        var barsByDate = new Dictionary<DateOnly, List<ParsedEquityBar>>();

        foreach (var line in File.ReadLines(path).Skip(1))
        {
            // Date, Symbol, Adj Close, Close, High, Low, Open, Volume
            var fields = line.Split(',');

            if (!tickers.Contains(fields[1]) || string.IsNullOrEmpty(fields[3]))
            {
                continue;
            }

            var date = DateOnly.ParseExact(fields[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (!barsByDate.TryGetValue(date, out var bars))
            {
                bars = [];
                barsByDate[date] = bars;
            }

            bars.Add(new ParsedEquityBar(
                fields[1],
                Open: Round(fields[6], EquityDecimals),
                High: Round(fields[4], EquityDecimals),
                Low: Round(fields[5], EquityDecimals),
                Close: Round(fields[3], EquityDecimals)));
        }

        return barsByDate;
    }

    /// <summary>Streams the file one day at a time: it is far too large to hold in memory.</summary>
    public static IEnumerable<ParsedFxDay> ReadEurUsdDays(string path, Func<DateOnly, bool> keepDate)
    {
        string? currentText = null;
        var keepCurrent = false;
        var currentDate = default(DateOnly);
        var bars = new List<ParsedFxBar>();

        foreach (var line in File.ReadLines(path).Skip(1))
        {
            // Date, Time, BO, BH, BL, BC, BCh, AO, AH, AL, AC, ACh
            var fields = line.Split(',');

            if (fields[0] != currentText)
            {
                if (keepCurrent)
                {
                    yield return new ParsedFxDay(currentDate, bars);
                }

                currentText = fields[0];
                currentDate = DateOnly.ParseExact(currentText, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                keepCurrent = keepDate(currentDate);
                bars = [];
            }

            if (keepCurrent)
            {
                AddBar(bars, new ParsedFxBar(ParseMinute(fields[1]), Round(fields[5], FxDecimals), Round(fields[10], FxDecimals)));
            }
        }

        if (keepCurrent)
        {
            yield return new ParsedFxDay(currentDate, bars);
        }
    }

    // A minute repeated in the file keeps its latest quote.
    private static void AddBar(List<ParsedFxBar> bars, ParsedFxBar bar)
    {
        if (bars.Count > 0 && bars[^1].Minute == bar.Minute)
        {
            bars[^1] = bar;
        }
        else
        {
            bars.Add(bar);
        }
    }

    // The time has no leading zero on the hour: 0:46, 18:29.
    private static short ParseMinute(string time)
    {
        var parts = time.Split(':');
        return (short)(int.Parse(parts[0], CultureInfo.InvariantCulture) * 60 + int.Parse(parts[1], CultureInfo.InvariantCulture));
    }

    private static decimal Round(string text, int decimals) =>
        Math.Round(ParseDecimal(text), decimals, MidpointRounding.AwayFromZero);

    private static decimal ParseDecimal(string text) =>
        decimal.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
}
