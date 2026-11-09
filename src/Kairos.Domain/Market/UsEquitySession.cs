namespace Kairos.Domain.Market;

/// <summary>The trading hours of the US equity market, in minutes since midnight UTC.</summary>
public readonly record struct UsEquitySession(int OpenMinute, int CloseMinute)
{
    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private static readonly TimeOnly LocalOpen = new(9, 30);

    /// <summary>The session starts at 9:30 in New York, so its UTC time depends on daylight saving.</summary>
    public static UsEquitySession For(DateOnly date)
    {
        var openUtc = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(LocalOpen, DateTimeKind.Unspecified), NewYork);
        var openMinute = (int)openUtc.TimeOfDay.TotalMinutes;

        return new UsEquitySession(openMinute, openMinute + IntradayPathGenerator.SessionMinutes);
    }
}
