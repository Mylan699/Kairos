using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Kairos.Infrastructure.Persistence;

/// <summary>Lets the EF Core tools build the model to generate migrations. No connection is opened.</summary>
public sealed class KairosDbContextFactory : IDesignTimeDbContextFactory<KairosDbContext>
{
    public KairosDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<KairosDbContext>()
            .UseSqlServer("Server=design-time-only;Database=Kairos")
            .Options;

        return new KairosDbContext(options);
    }
}
