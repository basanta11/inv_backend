// Inventory.Infrastructure/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using Inventory.Domain;

namespace Inventory.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Item> Items => Set<Item>();
    public DbSet<DemandStat> DemandStats => Set<DemandStat>();
    public DbSet<SupplierOrder> SupplierOrders => Set<SupplierOrder>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Item>().HasIndex(i => i.Sku).IsUnique();
        b.Entity<DemandStat>().HasIndex(d => new { d.ItemId, d.Day }).IsUnique();
        b.Entity<DemandStat>().HasOne(d => d.Item).WithMany()
            .HasForeignKey(d => d.ItemId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<SupplierOrder>().HasOne(o => o.Item).WithMany()
            .HasForeignKey(o => o.ItemId);
    }
}
