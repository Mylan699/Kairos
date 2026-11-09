using Microsoft.AspNetCore.SignalR;

namespace Kairos.Api.Realtime;

/// <summary>
/// Every connection receives ticks and trade changes. Positions and risk are heavier,
/// so a screen subscribes to them only while it displays them.
/// </summary>
public sealed class MarketHub : Hub
{
    public const string PositionsTopic = "positions";
    public const string RiskTopic = "risk";

    public Task Subscribe(string topic) => Groups.AddToGroupAsync(Context.ConnectionId, RequireKnown(topic));

    public Task Unsubscribe(string topic) => Groups.RemoveFromGroupAsync(Context.ConnectionId, RequireKnown(topic));

    private static string RequireKnown(string topic) =>
        topic is PositionsTopic or RiskTopic ? topic : throw new HubException("Unknown topic.");
}
