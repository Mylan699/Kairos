using Kairos.Infrastructure.Import;
using Kairos.Infrastructure.Persistence;
using Kairos.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Kairos.Infrastructure;

/// <summary>Brings the database up to date at startup: schema, then market data, then static data.</summary>
internal sealed class DatabaseInitializer(
    KairosDbContext db,
    MarketDataImporter importer,
    ReferenceSeeder referenceSeeder,
    IConfiguration configuration,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await db.Database.MigrateAsync(cancellationToken);

        var rawDataPath = configuration["MarketData:RawPath"];

        if (!await importer.IsCompleteAsync(cancellationToken))
        {
            if (string.IsNullOrEmpty(rawDataPath) || !MarketDataImporter.FilesExist(rawDataPath))
            {
                logger.LogWarning("No market data in the database and no raw files found at '{RawPath}'", rawDataPath);
                return;
            }

            await importer.ImportAsync(rawDataPath, cancellationToken);
        }

        await referenceSeeder.SeedAsync(cancellationToken);
    }
}
