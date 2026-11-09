using Kairos.Application.Abstractions;
using Kairos.Domain.Fx;
using Kairos.Domain.Market;
using Kairos.Domain.Rates;
using Kairos.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Kairos.Infrastructure.Persistence;

internal sealed class MarketHistoryRepository(KairosDbContext db, IReferenceDataProvider referenceData) : IMarketHistory
{
    public async Task<MarketSnapshot?> GetCloseAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var day = await db.EurUsdDailyBars.AsNoTracking().FirstOrDefaultAsync(row => row.Date == date, cancellationToken);

        return day is null ? null : (await BuildSnapshotsAsync([day], cancellationToken))[0];
    }

    public async Task<IReadOnlyList<MarketSnapshot>> GetClosesBeforeAsync(DateOnly date, int count, CancellationToken cancellationToken)
    {
        var days = await db.EurUsdDailyBars.AsNoTracking()
            .Where(row => row.Date < date)
            .OrderByDescending(row => row.Date)
            .Take(count)
            .ToListAsync(cancellationToken);

        days.Reverse();

        return days.Count == 0 ? [] : await BuildSnapshotsAsync(days, cancellationToken);
    }

    public async Task<IReadOnlyList<DailyBar>> GetDailyBarsBeforeAsync(
        Guid instrumentId,
        DateOnly date,
        int count,
        CancellationToken cancellationToken)
    {
        var catalog = await referenceData.GetCatalogAsync(cancellationToken);

        return catalog.Instrument(instrumentId).AssetClass switch
        {
            AssetClass.Equity => await EquityBarsAsync(instrumentId, date, count, cancellationToken),
            AssetClass.Fx => await EurUsdBarsAsync(date, count, cancellationToken),
            _ => [],
        };
    }

    private async Task<IReadOnlyList<DailyBar>> EquityBarsAsync(Guid instrumentId, DateOnly date, int count, CancellationToken cancellationToken)
    {
        var bars = await db.EquityDailyBars.AsNoTracking()
            .Where(row => row.InstrumentId == instrumentId && row.Date < date)
            .OrderByDescending(row => row.Date)
            .Take(count)
            .Select(row => new DailyBar(row.Date, row.Open, row.High, row.Low, row.Close))
            .ToListAsync(cancellationToken);

        bars.Reverse();
        return bars;
    }

    private async Task<IReadOnlyList<DailyBar>> EurUsdBarsAsync(DateOnly date, int count, CancellationToken cancellationToken)
    {
        var bars = await db.EurUsdDailyBars.AsNoTracking()
            .Where(row => row.Date < date)
            .OrderByDescending(row => row.Date)
            .Take(count)
            .Select(row => new DailyBar(row.Date, row.Open, row.High, row.Low, row.Close))
            .ToListAsync(cancellationToken);

        bars.Reverse();
        return bars;
    }

    /// <summary>Loads the equities and curves of a date range in two queries, then assembles one snapshot per day.</summary>
    private async Task<IReadOnlyList<MarketSnapshot>> BuildSnapshotsAsync(IReadOnlyList<EurUsdDailyBarRow> days, CancellationToken cancellationToken)
    {
        var first = days[0].Date;
        var last = days[^1].Date;

        var equities = await db.EquityDailyBars.AsNoTracking()
            .Where(row => row.Date >= first && row.Date <= last)
            .Select(row => new { row.Date, row.InstrumentId, row.Close })
            .ToListAsync(cancellationToken);

        var curves = await db.CurvePoints.AsNoTracking()
            .Where(row => row.Date >= first && row.Date <= last)
            .ToListAsync(cancellationToken);

        var equitiesByDate = equities.ToLookup(row => row.Date);
        var curvesByDate = curves.ToLookup(row => row.Date);

        return days
            .Select(day => new MarketSnapshot(
                day.Date,
                equitiesByDate[day.Date].ToDictionary(row => row.InstrumentId, row => row.Close),
                new EurUsdQuote(day.CloseBid, day.CloseAsk),
                new YieldCurve(curvesByDate[day.Date].Select(row => new CurvePoint(Tenor.FromMonths(row.Months), row.Yield)))))
            .ToList();
    }
}
