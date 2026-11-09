using Kairos.Infrastructure.Replay;

namespace Kairos.Infrastructure.Tests.Replay;

public class NightlyResetServiceTests
{
    [Fact]
    public void Before_the_hour_the_reset_is_today()
    {
        var now = new DateTimeOffset(2026, 1, 5, 1, 0, 0, TimeSpan.Zero);

        Assert.Equal(new DateTimeOffset(2026, 1, 5, 3, 0, 0, TimeSpan.Zero), NightlyResetService.NextRun(now, 3));
    }

    [Fact]
    public void After_the_hour_the_reset_is_tomorrow()
    {
        var now = new DateTimeOffset(2026, 1, 5, 3, 0, 1, TimeSpan.Zero);

        Assert.Equal(new DateTimeOffset(2026, 1, 6, 3, 0, 0, TimeSpan.Zero), NightlyResetService.NextRun(now, 3));
    }

    [Fact]
    public void A_local_time_is_understood_in_utc()
    {
        var now = new DateTimeOffset(2026, 1, 5, 2, 0, 0, TimeSpan.FromHours(2));

        Assert.Equal(new DateTimeOffset(2026, 1, 5, 3, 0, 0, TimeSpan.Zero), NightlyResetService.NextRun(now, 3));
    }
}
