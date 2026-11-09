using Kairos.Application.Abstractions;
using Kairos.Application.Common;
using Kairos.Application.Portfolio;
using Kairos.Domain.Market;
using Kairos.Domain.ValueObjects;

namespace Kairos.Application.Replay;

/// <param name="EquityBasketPercent">The average change of the equities since the previous close.</param>
/// <param name="TenYearPercent">The ten-year yield, in percent.</param>
/// <param name="DayPnl">The day P&amp;L of the desk, in euros.</param>
public sealed record IntradayPointDto(int Minute, decimal EurUsd, decimal EquityBasketPercent, decimal TenYearPercent, decimal DayPnl);

public sealed record IntradayPriceDto(int Minute, decimal Price);

/// <summary>What has happened so far on the replayed day, for screens opened halfway through it.</summary>
public sealed class IntradayService(IReplayControl control, PortfolioService portfolio, IReferenceDataProvider referenceData)
{
    private const int OverviewStepMinutes = 5;
    private const decimal TenYears = 10m;
    private const decimal FractionToPercent = 100m;

    public async Task<IReadOnlyList<IntradayPointDto>> GetOverviewAsync(CancellationToken cancellationToken)
    {
        var session = RequireSession();
        var state = await portfolio.GetStateAsync(cancellationToken);
        var baseline = state.PreviousCloseTotals.Values.Sum();

        return MinutesUpTo(session.Minute, OverviewStepMinutes)
            .Select(minute =>
            {
                var market = session.Day.SnapshotAt(minute);

                return new IntradayPointDto(
                    minute,
                    Math.Round(market.EurUsd.Mid, 5, MidpointRounding.AwayFromZero),
                    EquityBasketPercent(market, session.Day.PreviousClose),
                    Math.Round(market.Curve.YieldAt(TenYears) * FractionToPercent, 4, MidpointRounding.AwayFromZero),
                    Math.Round(PortfolioBuilder.TotalsAt(state.Positions, market).Values.Sum() - baseline, 2, MidpointRounding.AwayFromZero));
            })
            .ToList();
    }

    /// <summary>Equities only exist during their session. The other instruments trade all day.</summary>
    public async Task<IReadOnlyList<IntradayPriceDto>> GetPricesAsync(Guid instrumentId, CancellationToken cancellationToken)
    {
        var session = RequireSession();
        var catalog = await referenceData.GetCatalogAsync(cancellationToken);
        var instrument = catalog.Instrument(instrumentId);

        if (!instrument.IsTradableOn(session.Day.Date))
        {
            return [];
        }

        var isEquity = instrument.AssetClass == AssetClass.Equity;
        var first = isEquity ? session.Day.Session.OpenMinute : 0;
        var last = isEquity ? Math.Min(session.Minute, session.Day.Session.CloseMinute) : session.Minute;

        return Enumerable.Range(first, Math.Max(last - first + 1, 0))
            .Select(minute =>
            {
                var market = session.Day.SnapshotAt(minute);
                var price = instrument.AssetClass == AssetClass.Fx ? market.EurUsd.Mid : market.MarkPrice(instrument, 1m);

                return new IntradayPriceDto(minute, Math.Round(price, 5, MidpointRounding.AwayFromZero));
            })
            .ToList();
    }

    private ReplaySession RequireSession() => control.CurrentSession ?? throw new MarketNotReadyException();

    private static IEnumerable<int> MinutesUpTo(int lastMinute, int step)
    {
        for (var minute = 0; minute < lastMinute; minute += step)
        {
            yield return minute;
        }

        yield return lastMinute;
    }

    private static decimal EquityBasketPercent(MarketSnapshot market, MarketSnapshot previousClose)
    {
        if (market.EquityPrices.Count == 0)
        {
            return 0m;
        }

        var average = market.EquityPrices.Average(price => price.Value / previousClose.EquityPrices[price.Key] - 1m);
        return Math.Round(average * FractionToPercent, 3, MidpointRounding.AwayFromZero);
    }
}
