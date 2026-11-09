using Kairos.Application.Abstractions;
using Kairos.Infrastructure.Import;
using Kairos.Infrastructure.Persistence;
using Kairos.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kairos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Kairos")
            ?? throw new InvalidOperationException("The connection string 'Kairos' is missing.");

        // Retries on transient failures, which are routine on Azure SQL.
        services.AddDbContext<KairosDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<IMarketHistory, MarketHistoryRepository>();
        services.AddSingleton<IReferenceDataProvider, ReferenceDataProvider>();

        services.AddScoped<MarketDataImporter>();
        services.AddScoped<ReferenceSeeder>();
        services.AddScoped<DatabaseInitializer>();

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().InitializeAsync(cancellationToken);
    }
}
