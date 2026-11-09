using Kairos.Application.Replay;
using Microsoft.AspNetCore.Mvc;

namespace Kairos.Api.Controllers;

[ApiController]
[Route("api/replay")]
public sealed class ReplayController(ReplayService replay) : ControllerBase
{
    [HttpGet]
    public ReplayStatusDto Status() => replay.GetStatus();

    [HttpGet("days")]
    public IReadOnlyList<RemarkableDayDto> Days() => ReplayService.GetRemarkableDays();

    [HttpGet("speeds")]
    public IReadOnlyList<int> Speeds() => ReplayLimits.Speeds;

    [HttpPost("load")]
    public Task<ReplayStatusDto> Load(LoadReplayRequest request, CancellationToken cancellationToken) =>
        replay.LoadAsync(request, cancellationToken);

    [HttpPost("play")]
    public ReplayStatusDto Play() => replay.Play();

    [HttpPost("pause")]
    public ReplayStatusDto Pause() => replay.Pause();

    [HttpPost("speed")]
    public Task<ReplayStatusDto> Speed(SetReplaySpeedRequest request, CancellationToken cancellationToken) =>
        replay.SetSpeedAsync(request, cancellationToken);

    [HttpPost("seek")]
    public Task<ReplayStatusDto> Seek(SeekReplayRequest request, CancellationToken cancellationToken) =>
        replay.SeekAsync(request, cancellationToken);
}
