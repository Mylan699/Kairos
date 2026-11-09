namespace Kairos.Infrastructure.Replay;

/// <summary>Settings of the "Replay" configuration section.</summary>
public sealed class ReplayOptions
{
    public const string SectionName = "Replay";

    public int TickMilliseconds { get; set; } = 500;

    public DateOnly DefaultDate { get; set; } = new(2020, 3, 16);

    public int DefaultSpeed { get; set; } = 120;

    /// <summary>Starts just before the US equity open, when the day becomes interesting.</summary>
    public int DefaultStartMinute { get; set; } = 13 * 60;

    /// <summary>Back to the default day after this long without a command. Zero disables it.</summary>
    public int IdleResetMinutes { get; set; } = 20;

    public int NightlyResetHourUtc { get; set; } = 3;
}
