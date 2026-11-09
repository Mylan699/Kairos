using FluentValidation;
using Kairos.Application.Common;
using Kairos.Application.Replay;
using Kairos.Domain.Common;

namespace Kairos.Application.Tests.Replay;

public class ReplayServiceTests
{
    private static readonly DateOnly Date = new(2016, 6, 24);
    private static readonly CancellationToken None = CancellationToken.None;

    private readonly FakeControl control = new();

    private ReplayService Service() => new(
        control,
        new LoadReplayRequestValidator(),
        new SetReplaySpeedRequestValidator(),
        new SeekReplayRequestValidator());

    [Fact]
    public async Task Loading_a_day_returns_the_new_status()
    {
        var status = await Service().LoadAsync(new LoadReplayRequest(Date, 600), None);

        Assert.Equal(Date, status.Date);
        Assert.Equal(600, status.Minute);
    }

    [Fact]
    public async Task A_day_the_engine_refuses_is_a_business_error()
    {
        control.AcceptsLoad = false;

        await Assert.ThrowsAsync<DomainException>(() => Service().LoadAsync(new LoadReplayRequest(Date), None));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1_440)]
    public async Task A_start_outside_the_day_is_rejected(int minute)
    {
        await Assert.ThrowsAsync<ValidationException>(() => Service().LoadAsync(new LoadReplayRequest(Date, minute), None));
    }

    [Fact]
    public async Task Only_offered_speeds_are_accepted()
    {
        await Service().SetSpeedAsync(new SetReplaySpeedRequest(600), None);

        Assert.Equal(600, control.Status!.Speed);
        await Assert.ThrowsAsync<ValidationException>(() => Service().SetSpeedAsync(new SetReplaySpeedRequest(7), None));
    }

    [Fact]
    public async Task Seeking_moves_the_replay()
    {
        var status = await Service().SeekAsync(new SeekReplayRequest(900), None);

        Assert.Equal(900, status.Minute);
    }

    [Fact]
    public async Task Seeking_outside_the_day_is_rejected()
    {
        await Assert.ThrowsAsync<ValidationException>(() => Service().SeekAsync(new SeekReplayRequest(5_000), None));
    }

    [Fact]
    public void Play_and_pause_change_the_status()
    {
        Assert.False(Service().Pause().IsPlaying);
        Assert.True(Service().Play().IsPlaying);
    }

    [Fact]
    public void Status_before_the_first_load_means_not_ready()
    {
        control.Status = null;

        Assert.Throws<MarketNotReadyException>(() => Service().GetStatus());
    }

    [Fact]
    public void Every_remarkable_day_starts_within_the_day_and_appears_once()
    {
        var days = ReplayService.GetRemarkableDays();

        Assert.Equal(days.Count, days.Select(day => day.Date).Distinct().Count());
        Assert.All(days, day => Assert.InRange(day.StartMinute, 0, ReplayLimits.LastMinute));
    }

    private sealed class FakeControl : IReplayControl
    {
        public bool AcceptsLoad { get; set; } = true;

        public ReplayStatusDto? Status { get; set; } = new(Date, 0, 60, true, 810, 1200);

        public ReplaySession? CurrentSession => null;

        public Task<bool> LoadDayAsync(DateOnly date, int? startMinute, CancellationToken cancellationToken)
        {
            if (AcceptsLoad)
            {
                Status = Status! with { Date = date, Minute = startMinute ?? 0 };
            }

            return Task.FromResult(AcceptsLoad);
        }

        public void Play() => Status = Status! with { IsPlaying = true };

        public void Pause() => Status = Status! with { IsPlaying = false };

        public void SetSpeed(int speed) => Status = Status! with { Speed = speed };

        public void Seek(int minute) => Status = Status! with { Minute = minute };
    }
}
