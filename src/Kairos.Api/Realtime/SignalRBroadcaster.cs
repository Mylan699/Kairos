using Kairos.Application.Abstractions;
using Kairos.Application.Portfolio;
using Kairos.Application.Replay;
using Kairos.Application.Risk;
using Kairos.Application.Trades;
using Microsoft.AspNetCore.SignalR;

namespace Kairos.Api.Realtime;

internal sealed class SignalRBroadcaster(IHubContext<MarketHub> hub) : IMarketBroadcaster
{
    public Task PublishTickAsync(MarketTickDto tick, CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync("tick", tick, cancellationToken);

    public Task PublishPositionsAsync(IReadOnlyList<PositionDto> positions, CancellationToken cancellationToken) =>
        hub.Clients.Group(MarketHub.PositionsTopic).SendAsync("positions", positions, cancellationToken);

    public Task PublishRiskAsync(RiskReportDto report, CancellationToken cancellationToken) =>
        hub.Clients.Group(MarketHub.RiskTopic).SendAsync("risk", report, cancellationToken);

    public Task PublishDataResetAsync(CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync("dataReset", cancellationToken);
}

internal sealed class SignalRTradeNotifier(IHubContext<MarketHub> hub) : ITradeNotifier
{
    public Task TradeChangedAsync(TradeDto trade, CancellationToken cancellationToken) =>
        hub.Clients.All.SendAsync("tradeChanged", trade, cancellationToken);
}

/// <summary>The demo has no login: every action is recorded under the same name.</summary>
internal sealed class DemoActor : ICurrentActor
{
    public string Name => "Utilisateur démo";
}
