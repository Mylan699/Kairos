using Kairos.Infrastructure.Import;
using Kairos.Infrastructure.Persistence;
using Kairos.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Kairos.Infrastructure.Tests.Import;

[Collection(SqlServerCollection.Name)]
public sealed class MarketDataImporterTests(SqlServerFixture sql) : IDisposable
{
    private const int FullDayOfMinutes = 1_100;

    private static readonly DateOnly CompleteDay = new(2016, 6, 20);
    private static readonly DateOnly DayWithFewFxBars = new(2016, 6, 21);
    private static readonly DateOnly DayWithoutCurve = new(2016, 6, 22);
    private static readonly DateOnly DayWithMissingStock = new(2016, 6, 23);

    private readonly RawDataFolder folder = new();

    public void Dispose() => folder.Dispose();

    private void WriteFiles()
    {
        var curves = new List<string> { "date,US1M,US3M,US6M,US1Y,US2Y,US3Y,US5Y,US7Y,US10Y,US20Y,US30Y" };
        var stocks = new List<string> { "Date,Symbol,Adj Close,Close,High,Low,Open,Volume" };
        var fx = new List<string> { "Date,Time,BO,BH,BL,BC,BCh,AO,AH,AL,AC,ACh" };

        foreach (var date in new[] { CompleteDay, DayWithFewFxBars, DayWithoutCurve, DayWithMissingStock })
        {
            var text = date.ToString("yyyy-MM-dd");

            if (date != DayWithoutCurve)
            {
                curves.Add($"{text},1,1,1,1,1,1,2,2,2,3,3");
            }

            var tickers = ReferenceSeed.EquitySectors.Keys.Skip(date == DayWithMissingStock ? 1 : 0);
            stocks.AddRange(tickers.Select(ticker => $"{text},{ticker},9,10,11,9,10,100"));

            var minutes = date == DayWithFewFxBars ? 500 : FullDayOfMinutes;
            for (var minute = 0; minute < minutes; minute++)
            {
                fx.Add($"{text},{minute / 60}:{minute % 60:00},1.1,1.1,1.1,1.1000,0,1.1,1.1,1.1,1.1002,0");
            }
        }

        File.WriteAllLines(folder.TreasuryPath, curves);
        File.WriteAllLines(folder.StocksPath, stocks);
        File.WriteAllLines(folder.EurUsdPath, fx);
    }

    [Fact]
    public async Task Only_days_complete_in_all_three_sources_are_imported()
    {
        WriteFiles();
        await using var db = sql.CreateContext();
        var importer = new MarketDataImporter(db, NullLogger<MarketDataImporter>.Instance);

        await db.EurUsdDailyBars.ExecuteDeleteAsync();
        Assert.False(await importer.IsCompleteAsync(CancellationToken.None));

        await importer.ImportAsync(folder.Root, CancellationToken.None);

        Assert.True(await importer.IsCompleteAsync(CancellationToken.None));
        Assert.Equal([CompleteDay], await db.EurUsdDailyBars.Select(row => row.Date).ToListAsync());
        Assert.Equal(FullDayOfMinutes, await db.EurUsdMinuteBars.CountAsync(row => row.Date == CompleteDay));
        Assert.Equal(ReferenceSeed.EquitySectors.Count, await db.EquityDailyBars.CountAsync(row => row.Date == CompleteDay));
        Assert.Equal(11, await db.CurvePoints.CountAsync(row => row.Date == CompleteDay));
        Assert.Equal(0, await db.EurUsdMinuteBars.CountAsync(row => row.Date != CompleteDay));
    }

    [Fact]
    public async Task Daily_eur_usd_figures_summarize_the_minutes()
    {
        WriteFiles();
        await using var db = sql.CreateContext();

        await new MarketDataImporter(db, NullLogger<MarketDataImporter>.Instance).ImportAsync(folder.Root, CancellationToken.None);

        var day = await db.EurUsdDailyBars.AsNoTracking().SingleAsync();
        Assert.Equal(1.1001m, day.Close);
        Assert.Equal(1.1000m, day.CloseBid);
        Assert.Equal(1.1002m, day.CloseAsk);
    }

    [Fact]
    public void Files_are_detected_only_when_all_three_exist()
    {
        Assert.False(MarketDataImporter.FilesExist(folder.Root));

        WriteFiles();

        Assert.True(MarketDataImporter.FilesExist(folder.Root));
    }
}
