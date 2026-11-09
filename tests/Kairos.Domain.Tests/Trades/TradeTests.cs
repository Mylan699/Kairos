using Kairos.Domain.Common;
using Kairos.Domain.Trades;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.Trades;

public class TradeTests
{
    private const string Actor = "m.durand";
    private const decimal Rate = 1.13m;

    private static readonly DateTimeOffset BookedAt = new(2016, 6, 20, 14, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = BookedAt.AddMinutes(5);
    private static readonly Guid Instrument = Guid.NewGuid();
    private static readonly Guid Counterparty = Guid.NewGuid();
    private static readonly Guid BookId = Guid.NewGuid();
    private static readonly Guid Trader = Guid.NewGuid();

    private static TradeTerms Terms(decimal quantity = 100m, decimal price = 50m) => new(
        Instrument,
        Side.Buy,
        quantity,
        price,
        Counterparty,
        BookId,
        Trader,
        new DateOnly(2016, 6, 20),
        new DateOnly(2016, 6, 22),
        Rate);

    private static Trade NewTrade() => Trade.Book(Guid.NewGuid(), Terms(), Actor, BookedAt);

    [Fact]
    public void A_booked_trade_is_new_with_one_version_and_one_audit_line()
    {
        var trade = NewTrade();

        Assert.Equal(TradeStatus.New, trade.Status);
        Assert.Equal(1, trade.CurrentVersion.Number);
        var audit = Assert.Single(trade.AuditTrail);
        Assert.Equal(AuditAction.Booked, audit.Action);
        Assert.Equal(Actor, audit.Actor);
        Assert.Equal(BookedAt, audit.At);
    }

    [Fact]
    public void A_trade_goes_through_validation_then_settlement()
    {
        var trade = NewTrade();

        trade.Validate(Actor, Later);
        trade.Settle(Actor, Later);

        Assert.Equal(TradeStatus.Settled, trade.Status);
        Assert.Equal(
            [AuditAction.Booked, AuditAction.Validated, AuditAction.Settled],
            trade.AuditTrail.Select(entry => entry.Action));
    }

    [Fact]
    public void A_new_trade_cannot_be_settled_without_validation()
    {
        var trade = NewTrade();

        Assert.Throws<DomainException>(() => trade.Settle(Actor, Later));
        Assert.Equal(TradeStatus.New, trade.Status);
        Assert.Single(trade.AuditTrail);
    }

    [Fact]
    public void A_settled_trade_cannot_be_cancelled()
    {
        var trade = NewTrade();
        trade.Validate(Actor, Later);
        trade.Settle(Actor, Later);

        Assert.Throws<DomainException>(() => trade.Cancel("error", Actor, Later));
    }

    [Fact]
    public void Cancelling_records_the_reason_and_removes_the_trade_from_positions()
    {
        var trade = NewTrade();

        trade.Cancel("Booked on the wrong book", Actor, Later);

        Assert.False(trade.IsLive);
        Assert.Equal("Booked on the wrong book", trade.AuditTrail[^1].Details);
        Assert.Equal(TradeStatus.Cancelled, trade.AuditTrail[^1].ResultingStatus);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancelling_without_a_reason_is_rejected(string reason)
    {
        var trade = NewTrade();

        Assert.Throws<DomainException>(() => trade.Cancel(reason, Actor, Later));
        Assert.Equal(TradeStatus.New, trade.Status);
    }

    [Fact]
    public void An_amendment_adds_a_version_and_keeps_the_previous_one()
    {
        var trade = NewTrade();
        var original = trade.Terms;

        trade.Amend(Terms(quantity: 120m), "c.martin", Later);

        Assert.Equal(2, trade.CurrentVersion.Number);
        Assert.Equal(120m, trade.Terms.Quantity);
        Assert.Equal(original, trade.Versions[0].Terms);
        Assert.Equal("c.martin", trade.CurrentVersion.CreatedBy);
    }

    [Fact]
    public void The_audit_trail_lists_the_amended_fields()
    {
        var trade = NewTrade();

        trade.Amend(Terms(quantity: 120m, price: 50.5m), Actor, Later);

        var audit = trade.AuditTrail[^1];
        Assert.Equal(AuditAction.Amended, audit.Action);
        Assert.Equal(2, audit.Version);
        Assert.Equal("Quantity: 100 -> 120; Price: 50 -> 50.5", audit.Details);
    }

    [Fact]
    public void Amending_a_validated_trade_requires_a_new_validation()
    {
        var trade = NewTrade();
        trade.Validate(Actor, Later);

        trade.Amend(Terms(quantity: 120m), Actor, Later);

        Assert.Equal(TradeStatus.New, trade.Status);
        Assert.Equal(TradeStatus.New, trade.AuditTrail[^1].ResultingStatus);
    }

    [Fact]
    public void An_amendment_that_changes_nothing_is_rejected()
    {
        var trade = NewTrade();

        Assert.Throws<DomainException>(() => trade.Amend(Terms(), Actor, Later));
        Assert.Single(trade.Versions);
    }

    [Fact]
    public void A_settled_trade_cannot_be_amended()
    {
        var trade = NewTrade();
        trade.Validate(Actor, Later);
        trade.Settle(Actor, Later);

        Assert.Throws<DomainException>(() => trade.Amend(Terms(quantity: 120m), Actor, Later));
    }

    [Fact]
    public void An_action_without_an_actor_is_rejected()
    {
        Assert.Throws<DomainException>(() => Trade.Book(Guid.NewGuid(), Terms(), " ", BookedAt));
    }

    [Fact]
    public void A_rejected_action_leaves_the_trade_unchanged()
    {
        var trade = NewTrade();

        Assert.Throws<DomainException>(() => trade.Validate(" ", Later));
        Assert.Throws<DomainException>(() => trade.Amend(Terms(quantity: 120m), " ", Later));

        Assert.Equal(TradeStatus.New, trade.Status);
        Assert.Single(trade.Versions);
        Assert.Single(trade.AuditTrail);
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(-1, 50)]
    [InlineData(100, 0)]
    public void Non_positive_quantities_and_prices_are_rejected(double quantity, double price)
    {
        Assert.Throws<DomainException>(() => Terms((decimal)quantity, (decimal)price));
    }

    [Fact]
    public void A_value_date_before_the_trade_date_is_rejected()
    {
        Assert.Throws<DomainException>(() => new TradeTerms(
            Instrument, Side.Buy, 1m, 1m, Counterparty, BookId, Trader,
            new DateOnly(2016, 6, 22), new DateOnly(2016, 6, 21), Rate));
    }

    [Fact]
    public void A_value_date_on_a_weekend_is_rejected()
    {
        Assert.Throws<DomainException>(() => new TradeTerms(
            Instrument, Side.Buy, 1m, 1m, Counterparty, BookId, Trader,
            new DateOnly(2016, 6, 24), new DateOnly(2016, 6, 25), Rate));
    }

    [Fact]
    public void A_non_positive_rate_is_rejected()
    {
        Assert.Throws<DomainException>(() => new TradeTerms(
            Instrument, Side.Buy, 1m, 1m, Counterparty, BookId, Trader,
            new DateOnly(2016, 6, 20), new DateOnly(2016, 6, 22), 0m));
    }

    [Fact]
    public void Terms_convert_to_the_fill_seen_by_positions()
    {
        var fill = Terms(quantity: 10m, price: 20m).ToFill();

        Assert.Equal(new Kairos.Domain.Positions.Fill(Side.Buy, 10m, 20m, Rate), fill);
    }
}
