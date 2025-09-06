using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lazarus.Data.Design;

/// <summary>
/// Design-time factory for creating LazarusDbContext instances during migrations.
/// </summary>
public class LazarusDbContextFactory : IDesignTimeDbContextFactory<LazarusDbContext>
{
    public LazarusDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LazarusDbContext>();

        // Use a temporary database path for design-time operations
        var tempDbPath = Path.Combine(Path.GetTempPath(), "lazarus_design.db");
        var connectionString = $"Data Source={tempDbPath};Cache=Shared;";

        optionsBuilder.UseSqlite(connectionString, options =>
        {
            options.CommandTimeout(30);
        });

        optionsBuilder.EnableSensitiveDataLogging(false);
        optionsBuilder.EnableServiceProviderCaching();

        return new LazarusDbContext(optionsBuilder.Options);
    }
}