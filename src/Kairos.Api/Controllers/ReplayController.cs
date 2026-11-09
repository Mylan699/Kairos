using Kairos.Api.Security;
using Kairos.Application.Replay;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kairos.Api.Controllers;

[ApiController]
[Route("api/replay")]
public sealed class ReplayController(ReplayService replay, IntradayService intraday) : ControllerBase
{
    [HttpGet]
    public ReplayStatusDto Status() => replay.GetStatus();

    [HttpGet("days")]
    public IReadOnlyList<RemarkableDayDto> Days() => ReplayService.GetRemarkableDays();

    [HttpGet("speeds")]
    public IReadOnlyList<int> Speeds() => ReplayLimits.Speeds;

    [HttpGet("intraday")]
    public Task<IReadOnlyList<IntradayPointDto>> Overview(CancellationToken cancellationToken) =>
        intraday.GetOverviewAsync(cancellationToken);

    [HttpGet("intraday/{instrumentId:guid}")]
    public Task<IReadOnlyList<IntradayPriceDto>> Prices(Guid instrumentId, CancellationToken cancellationToken) =>
        intraday.GetPricesAsync(instrumentId, cancellationToken);

    [HttpPost("load")]
    [EnableRateLimiting(SecurityExtensions.WritesPolicy)]
    public Task<ReplayStatusDto> Load(LoadReplayRequest request, CancellationToken cancellationToken) =>
        replay.LoadAsync(request, cancellationToken);

    [HttpPost("play")]
    [EnableRateLimiting(SecurityExtensions.WritesPolicy)]
    public ReplayStatusDto Play() => replay.Play();

    [HttpPost("pause")]
    [EnableRateLimiting(SecurityExtensions.WritesPolicy)]
    public ReplayStatusDto Pause() => replay.Pause();

    [HttpPost("speed")]
    [EnableRateLimiting(SecurityExtensions.WritesPolicy)]
    public Task<ReplayStatusDto> Speed(SetReplaySpeedRequest request, CancellationToken cancellationToken) =>
        replay.SetSpeedAsync(request, cancellationToken);

    [HttpPost("seek")]
    [EnableRateLimiting(SecurityExtensions.WritesPolicy)]
    public Task<ReplayStatusDto> Seek(SeekReplayRequest request, CancellationToken cancellationToken) =>
        replay.SeekAsync(request, cancellationToken);
}
