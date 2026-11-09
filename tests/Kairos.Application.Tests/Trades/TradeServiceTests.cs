using FluentValidation;
using Kairos.Application.Common;
using Kairos.Domain.Common;
using Kairos.Domain.Trades;
using static Kairos.Application.Tests.Trades.TradeServiceFixture;

namespace Kairos.Application.Tests.Trades;

public class TradeServiceTests
{
    private readonly TradeServiceFixture fixture = new();
    private readonly CancellationToken none = CancellationToken.None;

    [Fact]
    public async Task Booking_defaults_to_the_market_date_and_its_settlement_date()
    {
        var trade = await fixture.Service.BookAsync(Request(), none);

        Assert.Equal(MarketDate, trade.TradeDate);
        Assert.Equal(new DateOnly(2016, 6, 28), trade.ValueDate);
        Assert.Equal(TradeStatus.New, trade.Status);
    }

    [Fact]
    public async Task A_booked_trade_shows_reference_names_rather_than_ids()
    {
        var trade = await fixture.Service.BookAsync(Request(), none);

        Assert.Equal("AAA", trade.Symbol);
        Assert.Equal("Equities", trade.Book);
        Assert.Equal("C. Martin", trade.Trader);
        Assert.Equal("Cpty Orion", trade.Counterparty);
    }

    [Fact]
    public async Task Booking_saves_once_and_notifies_connected_screens()
    {
        var trade = await fixture.Service.BookAsync(Request(), none);

        Assert.Equal(1, fixture.Trades.SaveCount);
        Assert.Equal(trade, Assert.Single(fixture.Notifier.Notifications));
    }

    [Fact]
    public async Task A_trade_of_the_day_is_converted_at_the_current_rate()
    {
        var booked = await fixture.Service.BookAsync(Request(), none);

        var trade = await fixture.Trades.FindAsync(booked.Id, none);

        Assert.Equal(CurrentRate, trade!.Terms.UsdPerEurAtTrade);
    }

    [Fact]
    public async Task A_back_dated_trade_is_converted_at_the_close_of_its_date()
    {
        var booked = await fixture.Service.BookAsync(Request(tradeDate: PastMarketDay), none);

        var trade = await fixture.Trades.FindAsync(booked.Id, none);

        Assert.Equal(PastRate, trade!.Terms.UsdPerEurAtTrade);
    }

    [Fact]
    public async Task A_trade_dated_on_a_non_market_day_is_rejected()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            fixture.Service.BookAsync(Request(tradeDate: new DateOnly(2016, 6, 19)), none));
    }

    [Fact]
    public async Task A_trade_dated_after_the_market_date_is_rejected()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            fixture.Service.BookAsync(Request(tradeDate: MarketDate.AddDays(1)), none));
    }

    [Fact]
    public async Task A_matured_bond_cannot_be_traded()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            fixture.Service.BookAsync(Request(instrumentId: MaturedBond.Id), none));
    }

    [Fact]
    public async Task An_unknown_instrument_is_reported_as_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            fixture.Service.BookAsync(Request(instrumentId: Guid.NewGuid()), none));
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(-1, 50)]
    [InlineData(100, 0)]
    public async Task An_invalid_request_is_rejected_before_anything_is_saved(double quantity, double price)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            fixture.Service.BookAsync(Request((decimal)quantity, (decimal)price), none));

        Assert.Equal(0, fixture.Trades.SaveCount);
    }

    [Fact]
    public async Task A_value_date_before_the_trade_date_is_rejected()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            fixture.Service.BookAsync(Request(tradeDate: PastMarketDay, valueDate: PastMarketDay.AddDays(-1)), none));
    }

    [Fact]
    public async Task An_amendment_returns_the_new_version()
    {
        var booked = await fixture.Service.BookAsync(Request(quantity: 100m), none);

        var amended = await fixture.Service.AmendAsync(booked.Id, Request(quantity: 150m), none);

        Assert.Equal(2, amended.Version);
        Assert.Equal(150m, amended.Quantity);
    }

    [Fact]
    public async Task The_detail_of_a_trade_contains_its_versions_and_audit_trail()
    {
        var booked = await fixture.Service.BookAsync(Request(quantity: 100m), none);
        await fixture.Service.AmendAsync(booked.Id, Request(quantity: 150m), none);

        var detail = await fixture.Service.GetAsync(booked.Id, none);

        Assert.Equal([100m, 150m], detail.Versions.Select(version => version.Quantity));
        Assert.Equal([AuditAction.Booked, AuditAction.Amended], detail.AuditTrail.Select(entry => entry.Action));
        Assert.Equal("demo.user", detail.AuditTrail[0].Actor);
        Assert.Equal(Now, detail.AuditTrail[0].At);
    }

    [Fact]
    public async Task A_trade_moves_through_validation_and_settlement()
    {
        var booked = await fixture.Service.BookAsync(Request(), none);

        await fixture.Service.ValidateAsync(booked.Id, none);
        var settled = await fixture.Service.SettleAsync(booked.Id, none);

        Assert.Equal(TradeStatus.Settled, settled.Status);
        Assert.Equal(3, fixture.Notifier.Notifications.Count);
    }

    [Fact]
    public async Task A_forbidden_transition_is_not_saved()
    {
        var booked = await fixture.Service.BookAsync(Request(), none);

        await Assert.ThrowsAsync<DomainException>(() => fixture.Service.SettleAsync(booked.Id, none));

        Assert.Equal(1, fixture.Trades.SaveCount);
    }

    [Fact]
    public async Task Cancelling_needs_a_reason()
    {
        var booked = await fixture.Service.BookAsync(Request(), none);

        await Assert.ThrowsAsync<ValidationException>(() =>
            fixture.Service.CancelAsync(booked.Id, new Kairos.Application.Trades.CancelTradeRequest(""), none));
    }

    [Fact]
    public async Task A_cancelled_trade_stays_in_the_blotter()
    {
        var booked = await fixture.Service.BookAsync(Request(), none);
        await fixture.Service.CancelAsync(booked.Id, new Kairos.Application.Trades.CancelTradeRequest("Duplicate"), none);

        var trades = await fixture.Service.ListAsync(none);

        Assert.Equal(TradeStatus.Cancelled, Assert.Single(trades).Status);
    }

    [Fact]
    public async Task Acting_on_an_unknown_trade_is_reported_as_not_found()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => fixture.Service.ValidateAsync(Guid.NewGuid(), none));
    }
}
