using Kairos.Infrastructure.Replay;

namespace Kairos.Infrastructure.Tests.Replay;

public class ReplayClockTests
{
    [Fact]
    public void Time_advances_by_speed_times_elapsed_time()
    {
        var clock = new ReplayClock(startMinute: 600, speed: 120);

        clock.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal(660, clock.Minute);
    }

    [Fact]
    public void A_paused_clock_does_not_advance()
    {
        var clock = new ReplayClock(600, 120);

        clock.Pause();
        clock.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal(600, clock.Minute);
        Assert.False(clock.IsPlaying);
    }

    [Fact]
    public void A_resumed_clock_advances_again()
    {
        var clock = new ReplayClock(600, 60);

        clock.Pause();
        clock.Play();
        clock.Advance(TimeSpan.FromSeconds(60));

        Assert.Equal(660, clock.Minute);
    }

    [Fact]
    public void The_end_of_the_day_starts_over_at_the_start_minute()
    {
        var clock = new ReplayClock(780, 1_800);

        clock.Seek(1_439);
        clock.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(780, clock.Minute);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(700, 700)]
    [InlineData(2_000, 1_439)]
    public void Seeking_stays_within_the_day(int target, int expected)
    {
        var clock = new ReplayClock(600, 60);

        clock.Seek(target);

        Assert.Equal(expected, clock.Minute);
    }
}
