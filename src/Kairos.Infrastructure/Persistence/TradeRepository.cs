using Kairos.Application.Abstractions;
using Kairos.Domain.Trades;
using Microsoft.EntityFrameworkCore;

namespace Kairos.Infrastructure.Persistence;

internal sealed class TradeRepository(KairosDbContext db) : ITradeRepository
{
    public Task<Trade?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Trades.FirstOrDefaultAsync(trade => trade.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Trade>> ListAsync(CancellationToken cancellationToken) =>
        await db.Trades.AsNoTracking().AsSplitQuery().ToListAsync(cancellationToken);

    public void Add(Trade trade) => db.Trades.Add(trade);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
