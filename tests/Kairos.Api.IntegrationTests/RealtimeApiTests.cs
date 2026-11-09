using System.Text.Json.Serialization;
using Kairos.Application.Market;
using Kairos.Application.Portfolio;
using Kairos.Application.Replay;
using Kairos.Application.Trades;
using Kairos.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Kairos.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class RealtimeApiTests(ApiFixture api) : IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

    private HubConnection connection = null!;

    public async Task InitializeAsync()
    {
        var server = api.Factory.Server;

        connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, "hubs/market"), options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            })
            .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

        await connection.StartAsync();
    }

    public async Task DisposeAsync() => await connection.DisposeAsync();

    private Task<T> NextMessageAsync<T>(string method)
    {
        var received = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<T>(method, message => received.TrySetResult(message));
        return received.Task;
    }

    [Fact]
    public async Task Every_connection_receives_ticks()
    {
        var tick = await NextMessageAsync<MarketTickDto>("tick").WaitAsync(Timeout);

        Assert.Equal(ApiFixture.ThirdDay, tick.Status.Date);
        Assert.Equal(11, tick.Curve.Points.Count);
        Assert.Contains(tick.Quotes, quote => quote.Symbol == "EUR/USD");
    }

    [Fact]
    public async Task Positions_are_sent_only_after_subscribing()
    {
        var positions = NextMessageAsync<List<PositionDto>>("positions");

        await connection.InvokeAsync("Subscribe", "positions");

        Assert.NotEmpty(await positions.WaitAsync(Timeout));
    }

    [Fact]
    public async Task An_unknown_topic_is_refused()
    {
        await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync("Subscribe", "secrets"));
    }

    [Fact]
    public async Task A_booked_trade_is_announced_to_connected_screens()
    {
        var reference = await api.GetAsync<ReferenceDataDto>("/api/market/reference");
        var changed = NextMessageAsync<TradeDto>("tradeChanged");

        var booked = await api.PostAsync<TradeDto>("/api/trades", new TradeRequest(
            reference.Instruments.First(item => item.AssetClass == AssetClass.Equity).Id,
            Side.Sell,
            10m,
            100m,
            reference.Counterparties[0].Id,
            reference.Books[1].Id,
            reference.Traders[0].Id));

        Assert.Equal(booked.Id, (await changed.WaitAsync(Timeout)).Id);
    }
}
