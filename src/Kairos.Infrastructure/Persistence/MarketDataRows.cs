namespace Kairos.Infrastructure.Persistence;

// Plain tables for imported market data. It is read-only once loaded, so it needs no domain behaviour.

public sealed class EquityDailyBarRow
{
    public Guid InstrumentId { get; set; }

    public DateOnly Date { get; set; }

    public decimal Open { get; set; }

    public decimal High { get; set; }

    public decimal Low { get; set; }

    public decimal Close { get; set; }
}

/// <summary>One row per market day: the presence of a row is what makes a date a market day.</summary>
public sealed class EurUsdDailyBarRow
{
    public DateOnly Date { get; set; }

    public decimal Open { get; set; }

    public decimal High { get; set; }

    public decimal Low { get; set; }

    public decimal Close { get; set; }

    public decimal CloseBid { get; set; }

    public decimal CloseAsk { get; set; }
}

public sealed class EurUsdMinuteBarRow
{
    public DateOnly Date { get; set; }

    /// <summary>Minutes since midnight UTC.</summary>
    public short Minute { get; set; }

    public decimal Bid { get; set; }

    public decimal Ask { get; set; }
}

public sealed class CurvePointRow
{
    public DateOnly Date { get; set; }

    public int Months { get; set; }

    /// <summary>The yield as a fraction: 0.025 means 2.5 %.</summary>
    public decimal Yield { get; set; }
}
