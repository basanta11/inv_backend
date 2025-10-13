using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inventory.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=inventory.db")
            .Options;

        return new AppDbContext(opts);
    }
}