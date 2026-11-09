using System.Data;
using Kairos.Infrastructure.Persistence;
using Kairos.Infrastructure.Seeding;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Kairos.Infrastructure.Import;

/// <summary>Loads the raw files into SQL Server, keeping only the days where all three sources are complete (ADR-004).</summary>
internal sealed class MarketDataImporter(KairosDbContext db, ILogger<MarketDataImporter> logger)
{
    private const int MinimumMinuteBars = 1_000;
    private const int MinuteBatchSize = 200_000;

    public static bool FilesExist(string rawPath) =>
        File.Exists(StocksPath(rawPath)) && File.Exists(EurUsdPath(rawPath)) && File.Exists(TreasuryPath(rawPath));

    /// <summary>The daily EUR/USD table is written last, so a row there proves that the import went to the end.</summary>
    public Task<bool> IsCompleteAsync(CancellationToken cancellationToken) => db.EurUsdDailyBars.AnyAsync(cancellationToken);

    public async Task ImportAsync(string rawPath, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reading curves and equities from {RawPath}", rawPath);

        var curves = CsvMarketDataReader.ReadCurves(TreasuryPath(rawPath));
        var tickers = ReferenceSeed.EquitySectors.Keys.ToHashSet();
        var equities = CsvMarketDataReader.ReadEquityBars(StocksPath(rawPath), tickers);

        var candidates = curves.Keys
            .Where(date => equities.TryGetValue(date, out var bars) && bars.Count == tickers.Count)
            .ToHashSet();

        await ClearAsync(cancellationToken);

        await using var connection = new SqlConnection(db.Database.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        var dailyBars = await ImportEurUsdMinutesAsync(connection, EurUsdPath(rawPath), candidates, cancellationToken);
        var marketDays = dailyBars.Select(row => row.Date).ToList();

        await BulkInsertAsync(connection, "EquityDailyBars", ToEquityTable(marketDays, equities), cancellationToken);
        await BulkInsertAsync(connection, "CurvePoints", ToCurveTable(marketDays, curves), cancellationToken);
        await BulkInsertAsync(connection, "EurUsdDailyBars", ToDailyTable(dailyBars), cancellationToken);

        logger.LogInformation("Imported {MarketDays} market days", marketDays.Count);
    }

    private static string StocksPath(string root) => Path.Combine(root, "sp500", "sp500_stocks.csv");

    private static string EurUsdPath(string root) => Path.Combine(root, "eurusd", "eurusd_minute.csv");

    private static string TreasuryPath(string root) => Path.Combine(root, "treasury", "us_treasury_yields_daily.csv");

    private async Task ClearAsync(CancellationToken cancellationToken)
    {
        await db.EurUsdMinuteBars.ExecuteDeleteAsync(cancellationToken);
        await db.EquityDailyBars.ExecuteDeleteAsync(cancellationToken);
        await db.CurvePoints.ExecuteDeleteAsync(cancellationToken);
    }

    private static async Task<List<EurUsdDailyBarRow>> ImportEurUsdMinutesAsync(
        SqlConnection connection,
        string path,
        HashSet<DateOnly> candidates,
        CancellationToken cancellationToken)
    {
        var dailyBars = new List<EurUsdDailyBarRow>();
        var minutes = NewMinuteTable();

        foreach (var day in CsvMarketDataReader.ReadEurUsdDays(path, candidates.Contains))
        {
            if (day.Bars.Count < MinimumMinuteBars)
            {
                continue;
            }

            dailyBars.Add(Summarize(day));

            var date = ToDateTime(day.Date);
            foreach (var bar in day.Bars)
            {
                minutes.Rows.Add(date, bar.Minute, bar.Bid, bar.Ask);
            }

            if (minutes.Rows.Count >= MinuteBatchSize)
            {
                await BulkInsertAsync(connection, "EurUsdMinuteBars", minutes, cancellationToken);
                minutes.Clear();
            }
        }

        await BulkInsertAsync(connection, "EurUsdMinuteBars", minutes, cancellationToken);

        return dailyBars;
    }

    private static EurUsdDailyBarRow Summarize(ParsedFxDay day)
    {
        var mids = day.Bars.Select(bar => Mid(bar)).ToList();
        var last = day.Bars[^1];

        return new EurUsdDailyBarRow
        {
            Date = day.Date,
            Open = mids[0],
            High = mids.Max(),
            Low = mids.Min(),
            Close = mids[^1],
            CloseBid = last.Bid,
            CloseAsk = last.Ask,
        };
    }

    private static decimal Mid(ParsedFxBar bar) => Math.Round((bar.Bid + bar.Ask) / 2m, 5, MidpointRounding.AwayFromZero);

    private static DataTable NewMinuteTable()
    {
        var table = new DataTable();
        table.Columns.Add("Date", typeof(DateTime));
        table.Columns.Add("Minute", typeof(short));
        table.Columns.Add("Bid", typeof(decimal));
        table.Columns.Add("Ask", typeof(decimal));
        return table;
    }

    private static DataTable ToEquityTable(IEnumerable<DateOnly> marketDays, Dictionary<DateOnly, List<ParsedEquityBar>> equities)
    {
        var table = new DataTable();
        table.Columns.Add("InstrumentId", typeof(Guid));
        table.Columns.Add("Date", typeof(DateTime));
        table.Columns.Add("Open", typeof(decimal));
        table.Columns.Add("High", typeof(decimal));
        table.Columns.Add("Low", typeof(decimal));
        table.Columns.Add("Close", typeof(decimal));

        foreach (var date in marketDays)
        {
            foreach (var bar in equities[date])
            {
                table.Rows.Add(ReferenceSeed.EquityId(bar.Ticker), ToDateTime(date), bar.Open, bar.High, bar.Low, bar.Close);
            }
        }

        return table;
    }

    private static DataTable ToCurveTable(IEnumerable<DateOnly> marketDays, Dictionary<DateOnly, Dictionary<int, decimal>> curves)
    {
        var table = new DataTable();
        table.Columns.Add("Date", typeof(DateTime));
        table.Columns.Add("Months", typeof(int));
        table.Columns.Add("Yield", typeof(decimal));

        foreach (var date in marketDays)
        {
            foreach (var (months, value) in curves[date])
            {
                table.Rows.Add(ToDateTime(date), months, value);
            }
        }

        return table;
    }

    private static DataTable ToDailyTable(IEnumerable<EurUsdDailyBarRow> rows)
    {
        var table = new DataTable();
        table.Columns.Add("Date", typeof(DateTime));
        table.Columns.Add("Open", typeof(decimal));
        table.Columns.Add("High", typeof(decimal));
        table.Columns.Add("Low", typeof(decimal));
        table.Columns.Add("Close", typeof(decimal));
        table.Columns.Add("CloseBid", typeof(decimal));
        table.Columns.Add("CloseAsk", typeof(decimal));

        foreach (var row in rows)
        {
            table.Rows.Add(ToDateTime(row.Date), row.Open, row.High, row.Low, row.Close, row.CloseBid, row.CloseAsk);
        }

        return table;
    }

    private static DateTime ToDateTime(DateOnly date) => date.ToDateTime(TimeOnly.MinValue);

    private static async Task BulkInsertAsync(SqlConnection connection, string tableName, DataTable data, CancellationToken cancellationToken)
    {
        using var bulkCopy = new SqlBulkCopy(connection) { DestinationTableName = tableName, BulkCopyTimeout = 0 };

        foreach (DataColumn column in data.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(data, cancellationToken);
    }
}
