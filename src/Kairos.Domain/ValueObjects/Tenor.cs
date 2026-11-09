using Kairos.Domain.Common;

namespace Kairos.Domain.ValueObjects;

/// <summary>A standard maturity point of the US Treasury yield curve.</summary>
public sealed record Tenor
{
    public static readonly Tenor OneMonth = new(1);
    public static readonly Tenor ThreeMonths = new(3);
    public static readonly Tenor SixMonths = new(6);
    public static readonly Tenor OneYear = new(12);
    public static readonly Tenor TwoYears = new(24);
    public static readonly Tenor ThreeYears = new(36);
    public static readonly Tenor FiveYears = new(60);
    public static readonly Tenor SevenYears = new(84);
    public static readonly Tenor TenYears = new(120);
    public static readonly Tenor TwentyYears = new(240);
    public static readonly Tenor ThirtyYears = new(360);

    /// <summary>Every curve point, from the shortest maturity to the longest.</summary>
    public static IReadOnlyList<Tenor> CurvePoints { get; } =
    [
        OneMonth, ThreeMonths, SixMonths, OneYear, TwoYears, ThreeYears,
        FiveYears, SevenYears, TenYears, TwentyYears, ThirtyYears,
    ];

    private Tenor(int months) => Months = months;

    public int Months { get; }

    public decimal Years => Months / 12m;

    public string Label => Months < 12 ? $"{Months}M" : $"{Months / 12}Y";

    public static Tenor Parse(string label) =>
        CurvePoints.FirstOrDefault(tenor => string.Equals(tenor.Label, label, StringComparison.OrdinalIgnoreCase))
        ?? throw new DomainException($"Unknown tenor '{label}'.");

    public static Tenor FromMonths(int months) =>
        CurvePoints.FirstOrDefault(tenor => tenor.Months == months)
        ?? throw new DomainException($"No tenor of {months} months.");

    public override string ToString() => Label;
}
