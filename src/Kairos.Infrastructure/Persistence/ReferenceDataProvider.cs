using Kairos.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kairos.Infrastructure.Persistence;

/// <summary>Loads the static data once. An empty database is not cached, as it may be seeded later.</summary>
internal sealed class ReferenceDataProvider(IServiceScopeFactory scopeFactory) : IReferenceDataProvider
{
    private ReferenceCatalog? catalog;

    public async Task<ReferenceCatalog> GetCatalogAsync(CancellationToken cancellationToken)
    {
        if (catalog is not null)
        {
            return catalog;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<KairosDbContext>();

        var loaded = new ReferenceCatalog(
            await db.Instruments.AsNoTracking().ToListAsync(cancellationToken),
            await db.Books.AsNoTracking().ToListAsync(cancellationToken),
            await db.Traders.AsNoTracking().ToListAsync(cancellationToken),
            await db.Counterparties.AsNoTracking().ToListAsync(cancellationToken));

        if (loaded.Instruments.Count > 0)
        {
            catalog = loaded;
        }

        return loaded;
    }
}
