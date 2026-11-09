using Kairos.Domain.Trades;
using Kairos.Domain.ValueObjects;
using Kairos.Infrastructure.Persistence;

namespace Kairos.Infrastructure.Tests.Persistence;

[Collection(SqlServerCollection.Name)]
public class TradeRepositoryTests(SqlServerFixture sql)
{
    private static readonly DateTimeOffset At = new(2016, 6, 20, 14, 30, 0, TimeSpan.Zero);
    private readonly CancellationToken none = CancellationToken.None;

    private static readonly Guid Instrument = Guid.NewGuid();
    private static readonly Guid Counterparty = Guid.NewGuid();
    private static readonly Guid Book = Guid.NewGuid();
    private static readonly Guid Trader = Guid.NewGuid();

    private static TradeTerms Terms(decimal quantity = 100.5m) => new(
        Instrument,
        Side.Sell,
        quantity,
        123.456789m,
        Counterparty,
        Book,
        Trader,
        new DateOnly(2016, 6, 20),
        new DateOnly(2016, 6, 22),
        1.12345678m);

    private async Task<Trade> SavedTradeAsync()
    {
        var trade = Trade.Book(Guid.NewGuid(), Terms(), "m.durand", At);

        await using var db = sql.CreateContext();
        var repository = new TradeRepository(db);
        repository.Add(trade);
        await repository.SaveChangesAsync(none);

        return trade;
    }

    [Fact]
    public async Task A_saved_trade_comes_back_with_all_its_terms()
    {
        var trade = await SavedTradeAsync();

        await using var db = sql.CreateContext();
        var loaded = await new TradeRepository(db).FindAsync(trade.Id, none);

        Assert.NotNull(loaded);
        Assert.Equal(TradeStatus.New, loaded.Status);
        Assert.Equal(trade.Terms, loaded.Terms);
    }

    [Fact]
    public async Task Amendments_and_status_changes_keep_the_whole_history_in_order()
    {
        var trade = await SavedTradeAsync();
        var firstTerms = trade.Terms;

        await using (var db = sql.CreateContext())
        {
            var repository = new TradeRepository(db);
            var tracked = await repository.FindAsync(trade.Id, none);
            tracked!.Amend(Terms(quantity: 250m), "c.martin", At.AddMinutes(1));
            tracked.Validate("c.martin", At.AddMinutes(2));
            tracked.Settle("c.martin", At.AddMinutes(3));
            await repository.SaveChangesAsync(none);
        }

        await using var readDb = sql.CreateContext();
        var loaded = await new TradeRepository(readDb).FindAsync(trade.Id, none);

        Assert.Equal(TradeStatus.Settled, loaded!.Status);
        Assert.Equal([1, 2], loaded.Versions.Select(version => version.Number));
        Assert.Equal(firstTerms, loaded.Versions[0].Terms);
        Assert.Equal(250m, loaded.Terms.Quantity);
        Assert.Equal(
            [AuditAction.Booked, AuditAction.Amended, AuditAction.Validated, AuditAction.Settled],
            loaded.AuditTrail.Select(entry => entry.Action));
        Assert.Equal("Quantity: 100.5 -> 250", loaded.AuditTrail[1].Details);
    }

    [Fact]
    public async Task Listing_returns_trades_ready_for_position_building()
    {
        var trade = await SavedTradeAsync();

        await using var db = sql.CreateContext();
        var trades = await new TradeRepository(db).ListAsync(none);

        var listed = Assert.Single(trades, item => item.Id == trade.Id);
        Assert.Equal(trade.Terms.ToFill(), listed.Terms.ToFill());
        Assert.Single(listed.AuditTrail);
    }
}
