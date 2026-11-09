using Kairos.Application.Abstractions;
using Kairos.Domain.Fx;
using Kairos.Domain.Market;
using Kairos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kairos.Infrastructure.Replay;

/// <summary>Reads what a replay needs for one date and generates the intraday equity paths.</summary>
internal sealed class ReplayDayLoader(KairosDbContext db, IMarketHistory history, IReferenceDataProvider referenceData)
{
    /// <returns>The day, or null when the date cannot be replayed because it has no previous close.</returns>
    public async Task<ReplayDay?> LoadAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var close = await history.GetCloseAsync(date, cancellationToken);
        var previousCloses = await history.GetClosesBeforeAsync(date, 1, cancellationToken);

        if (close is null || previousCloses.Count == 0)
        {
            return null;
        }

        var catalog = await referenceData.GetCatalogAsync(cancellationToken);

        var bars = await db.EquityDailyBars.AsNoTracking()
            .Where(row => row.Date == date)
            .ToListAsync(cancellationToken);

        var paths = bars.ToDictionary(
            bar => bar.InstrumentId,
            bar => IntradayPathGenerator.Generate(
                bar.Open,
                bar.High,
                bar.Low,
                bar.Close,
                IntradayPathGenerator.SeedFor(catalog.Instrument(bar.InstrumentId).Symbol, date)));

        var minutes = await db.EurUsdMinuteBars.AsNoTracking()
            .Where(row => row.Date == date)
            .OrderBy(row => row.Minute)
            .ToListAsync(cancellationToken);

        var ticks = minutes.Select(row => new FxTick(row.Minute, new EurUsdQuote(row.Bid, row.Ask))).ToArray();

        return new ReplayDay(date, previousCloses[0], paths, ticks, close.Curve);
    }
}
