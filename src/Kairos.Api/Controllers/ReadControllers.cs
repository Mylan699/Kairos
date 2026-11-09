using Kairos.Application.Abstractions;
using Kairos.Application.Market;
using Kairos.Application.Portfolio;
using Kairos.Application.Risk;
using Microsoft.AspNetCore.Mvc;

namespace Kairos.Api.Controllers;

[ApiController]
[Route("api/portfolio")]
public sealed class PortfolioController(PortfolioService portfolio, DashboardService dashboard) : ControllerBase
{
    [HttpGet("positions")]
    public Task<IReadOnlyList<PositionDto>> Positions(CancellationToken cancellationToken) =>
        portfolio.GetPositionsAsync(cancellationToken);

    [HttpGet("summary")]
    public Task<PortfolioSummaryDto> Summary(CancellationToken cancellationToken) =>
        portfolio.GetSummaryAsync(cancellationToken);

    [HttpGet("dashboard")]
    public Task<DashboardDto> Dashboard(CancellationToken cancellationToken) => dashboard.GetAsync(cancellationToken);
}

[ApiController]
[Route("api/risk")]
public sealed class RiskController(RiskService risk) : ControllerBase
{
    [HttpGet]
    public Task<RiskReportDto> Report(CancellationToken cancellationToken) => risk.GetReportAsync(cancellationToken);

    [HttpGet("scenarios")]
    public IReadOnlyList<HistoricalScenarioDto> Scenarios() => HistoricalScenarios.All;

    [HttpPost("stress/parametric")]
    public Task<StressResultDto> Parametric(ParametricStressRequest request, CancellationToken cancellationToken) =>
        risk.RunParametricAsync(request, cancellationToken);

    [HttpPost("stress/historical")]
    public Task<StressResultDto> Historical(HistoricalStressRequest request, CancellationToken cancellationToken) =>
        risk.RunHistoricalAsync(request, cancellationToken);
}

[ApiController]
[Route("api/market")]
public sealed class MarketController(MarketService market) : ControllerBase
{
    private const int DefaultBarCount = 250;

    [HttpGet("reference")]
    public Task<ReferenceDataDto> Reference(CancellationToken cancellationToken) =>
        market.GetReferenceDataAsync(cancellationToken);

    [HttpGet("instruments/{id:guid}/bars")]
    public Task<IReadOnlyList<DailyBar>> Bars(Guid id, CancellationToken cancellationToken, [FromQuery] int count = DefaultBarCount) =>
        market.GetDailyBarsAsync(id, count, cancellationToken);

    [HttpGet("curve")]
    public Task<CurveDto> Curve([FromQuery] DateOnly? date, CancellationToken cancellationToken) =>
        market.GetCurveAsync(date, cancellationToken);
}
