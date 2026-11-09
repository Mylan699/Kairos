using Kairos.Domain.Fx;
using Kairos.Domain.Rates;

namespace Kairos.Domain.Market;

/// <param name="Minute">Minutes since midnight UTC.</param>
public readonly record struct FxTick(int Minute, EurUsdQuote Quote);

/// <summary>Everything needed to rebuild the market of one day at any minute.</summary>
public sealed class ReplayDay
{
    public const int MinutesPerDay = 1_440;

    private readonly IReadOnlyDictionary<Guid, decimal[]> equityPaths;
    private readonly FxTick[] fxTicks;
    private readonly YieldCurve closeCurve;

    /// <param name="date">The day being replayed.</param>
    /// <param name="previousClose">The market at the close of the previous market day.</param>
    /// <param name="equityPaths">One price per session minute and per equity.</param>
    /// <param name="fxTicks">The real EUR/USD quotes of the day, in time order.</param>
    /// <param name="closeCurve">The yield curve published for the day.</param>
    public ReplayDay(
        DateOnly date,
        MarketSnapshot previousClose,
        IReadOnlyDictionary<Guid, decimal[]> equityPaths,
        FxTick[] fxTicks,
        YieldCurve closeCurve)
    {
        Date = date;
        PreviousClose = previousClose;
        this.equityPaths = equityPaths;
        this.fxTicks = fxTicks;
        this.closeCurve = closeCurve;
        Session = UsEquitySession.For(date);
    }

    public DateOnly Date { get; }

    public MarketSnapshot PreviousClose { get; }

    public UsEquitySession Session { get; }

    public MarketSnapshot SnapshotAt(int minute)
    {
        var prices = equityPaths.ToDictionary(path => path.Key, path => EquityPriceAt(path.Key, path.Value, minute));
        var curve = PreviousClose.Curve.BlendedWith(closeCurve, (decimal)minute / MinutesPerDay);

        return new MarketSnapshot(Date, prices, EurUsdAt(minute), curve);
    }

    private decimal EquityPriceAt(Guid instrumentId, decimal[] path, int minute)
    {
        if (minute < Session.OpenMinute)
        {
            return PreviousClose.EquityPrices[instrumentId];
        }

        return path[Math.Min(minute - Session.OpenMinute, path.Length - 1)];
    }

    private EurUsdQuote EurUsdAt(int minute)
    {
        var index = Array.FindLastIndex(fxTicks, tick => tick.Minute <= minute);
        return index >= 0 ? fxTicks[index].Quote : PreviousClose.EurUsd;
    }
}
