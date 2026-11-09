using System.Net;
using System.Net.Http.Json;
using Kairos.Application.Market;
using Kairos.Application.Portfolio;
using Kairos.Application.Trades;
using Kairos.Domain.Trades;
using Kairos.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Kairos.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class TradesApiTests(ApiFixture api)
{
    private async Task<TradeRequest> TicketAsync(decimal quantity = 100m)
    {
        var reference = await api.GetAsync<ReferenceDataDto>("/api/market/reference");

        return new TradeRequest(
            reference.Instruments.First(item => item.AssetClass == AssetClass.Equity).Id,
            Side.Buy,
            quantity,
            101.5m,
            reference.Counterparties[0].Id,
            reference.Books[0].Id,
            reference.Traders[0].Id);
    }

    private async Task<TradeDto> BookAsync(decimal quantity = 100m) =>
        await api.PostAsync<TradeDto>("/api/trades", await TicketAsync(quantity));

    [Fact]
    public async Task The_blotter_contains_the_seeded_trades()
    {
        var trades = await api.GetAsync<List<TradeDto>>("/api/trades");

        Assert.InRange(trades.Count, 2_800, 3_300);
        Assert.All(trades.Take(20), trade => Assert.False(string.IsNullOrEmpty(trade.Book)));
    }

    [Fact]
    public async Task Booking_returns_created_with_the_location_of_the_trade()
    {
        var response = await api.Client.PostAsJsonAsync("/api/trades", await TicketAsync(), ApiFixture.Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var trade = await response.Content.ReadFromJsonAsync<TradeDto>(ApiFixture.Json);
        Assert.EndsWith($"/api/trades/{trade!.Id}", response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(ApiFixture.ThirdDay, trade.TradeDate);
    }

    [Fact]
    public async Task The_whole_lifecycle_is_recorded_in_the_detail()
    {
        var trade = await BookAsync();

        var amended = (await api.Client.PutAsJsonAsync($"/api/trades/{trade.Id}", await TicketAsync(quantity: 150m), ApiFixture.Json))
            .Content.ReadFromJsonAsync<TradeDto>(ApiFixture.Json);
        Assert.Equal(2, (await amended)!.Version);

        await api.PostAsync<TradeDto>($"/api/trades/{trade.Id}/validate");
        var settled = await api.PostAsync<TradeDto>($"/api/trades/{trade.Id}/settle");
        Assert.Equal(TradeStatus.Settled, settled.Status);

        var detail = await api.GetAsync<TradeDetailDto>($"/api/trades/{trade.Id}");
        Assert.Equal(2, detail.Versions.Count);
        Assert.Equal(
            [AuditAction.Booked, AuditAction.Amended, AuditAction.Validated, AuditAction.Settled],
            detail.AuditTrail.Select(entry => entry.Action));
        Assert.Equal("Utilisateur démo", detail.AuditTrail[0].Actor);
    }

    [Fact]
    public async Task A_forbidden_transition_is_a_problem_details_422()
    {
        var trade = await BookAsync();

        var response = await api.Client.PostAsync($"/api/trades/{trade.Id}/settle", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiFixture.Json);
        Assert.Contains("cannot move", problem!.Detail);
    }

    [Fact]
    public async Task An_invalid_ticket_is_a_validation_problem()
    {
        var response = await api.Client.PostAsJsonAsync("/api/trades", await TicketAsync(quantity: 0m), ApiFixture.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiFixture.Json);
        Assert.Contains("Quantity", problem!.Errors.Keys);
    }

    [Fact]
    public async Task An_unknown_trade_is_a_404()
    {
        var response = await api.Client.GetAsync($"/api/trades/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Malformed_json_is_a_bad_request()
    {
        var response = await api.Client.PostAsync("/api/trades", new StringContent("{", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task An_estimate_shows_the_impact_without_booking()
    {
        var before = (await api.GetAsync<List<TradeDto>>("/api/trades")).Count;

        var impact = await api.PostAsync<TradeImpactDto>("/api/trades/estimate", await TicketAsync());

        Assert.True(impact.NotionalEur > 0m);
        Assert.Equal(before, (await api.GetAsync<List<TradeDto>>("/api/trades")).Count);
    }

    [Fact]
    public async Task A_booked_trade_moves_the_position()
    {
        var ticket = await TicketAsync(quantity: 1_000m);
        var positions = await api.GetAsync<List<PositionDto>>("/api/portfolio/positions");
        var before = positions.SingleOrDefault(item => item.BookId == ticket.BookId && item.InstrumentId == ticket.InstrumentId)?.Quantity ?? 0m;

        await api.PostAsync<TradeDto>("/api/trades", ticket);

        var updated = await api.GetAsync<List<PositionDto>>("/api/portfolio/positions");
        Assert.Equal(before + 1_000m, updated.Single(item => item.BookId == ticket.BookId && item.InstrumentId == ticket.InstrumentId).Quantity);
    }
}
