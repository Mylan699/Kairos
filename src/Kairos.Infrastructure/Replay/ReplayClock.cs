using Kairos.Domain.Market;

namespace Kairos.Infrastructure.Replay;

/// <summary>The simulated time of the replay. It knows nothing about markets or threads.</summary>
internal sealed class ReplayClock(int startMinute, int speed)
{
    private const double SecondsPerMinute = 60d;

    private double minute = startMinute;

    public int Minute => (int)minute;

    public int Speed { get; set; } = speed;

    public bool IsPlaying { get; private set; } = true;

    public void Play() => IsPlaying = true;

    public void Pause() => IsPlaying = false;

    public void Seek(int target) => minute = Math.Clamp(target, 0, ReplayDay.MinutesPerDay - 1);

    /// <summary>Moves time forward. The end of the day starts it over, so that the demo never stops.</summary>
    public void Advance(TimeSpan realElapsed)
    {
        if (!IsPlaying)
        {
            return;
        }

        minute += Speed * realElapsed.TotalSeconds / SecondsPerMinute;

        if (minute >= ReplayDay.MinutesPerDay)
        {
            minute = startMinute;
        }
    }
}
