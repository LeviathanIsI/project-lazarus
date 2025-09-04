using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lazarus.App.Data.Design;

/// <summary>
/// Design-time factory for creating LazarusDbContext instances during migrations
/// </summary>
public class LazarusDbContextFactory : IDesignTimeDbContextFactory<LazarusDbContext>
{
    /// <summary>
    /// Creates a new instance of LazarusDbContext for design-time operations
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>A configured LazarusDbContext instance</returns>
    public LazarusDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LazarusDbContext>();
        
        // Use a default connection string for design-time operations
        optionsBuilder.UseSqlite("Data Source=lazarus.db");
        
        return new LazarusDbContext(optionsBuilder.Options);
    }
}