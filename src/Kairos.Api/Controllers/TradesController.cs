using Kairos.Api.Security;
using Kairos.Application.Portfolio;
using Kairos.Application.Trades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kairos.Api.Controllers;

[ApiController]
[Route("api/trades")]
public sealed class TradesController(TradeService trades, PortfolioService portfolio) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<TradeDto>> List(CancellationToken cancellationToken) => trades.ListAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<TradeDetailDto> Get(Guid id, CancellationToken cancellationToken) => trades.GetAsync(id, cancellationToken);

    [HttpPost]
    [EnableRateLimiting(SecurityExtensions.WritesPolicy)]
    public async Task<ActionResult<TradeDto>> Book(TradeRequest request, CancellationToken cancellationToken)
    {
        var trade = await trades.BookAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = trade.Id }, trade);
    }

    [HttpPost("estimate")]
    public Task<TradeImpactDto> Estimate(TradeRequest request, CancellationToken cancellationToken) =>
        portfolio.EstimateImpactAsync(request, cancellationToken);

    [HttpPut("{id:guid}")]
    [EnableRateLimiting(SecurityExtensions.WritesPolicy)]
    public Task<TradeDto> Amend(Guid id, TradeRequest request, CancellationToken cancellationToken) =>
        trades.AmendAsync(id, request, cancellationToken);

    [HttpPost("{id:guid}/validate")]
    [EnableRateLimiting(SecurityExtensions.WritesPolicy)]
    public Task<TradeDto> Validate(Guid id, CancellationToken cancellationToken) => trades.ValidateAsync(id, cancellationToken);

    [HttpPost("{id:guid}/settle")]
    [EnableRateLimiting(SecurityExtensions.WritesPolicy)]
    public Task<TradeDto> Settle(Guid id, CancellationToken cancellationToken) => trades.SettleAsync(id, cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    [EnableRateLimiting(SecurityExtensions.WritesPolicy)]
    public Task<TradeDto> Cancel(Guid id, CancelTradeRequest request, CancellationToken cancellationToken) =>
        trades.CancelAsync(id, request, cancellationToken);
}
