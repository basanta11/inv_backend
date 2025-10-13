using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!db.Items.Any())
        {
            var items = new List<Item>
            {
                new() { Id = Guid.NewGuid(), Name = "Apple iPhone 15", Sku = "IPH15", Stock = 50, ComputedReorderPoint = 20, ManualReorderPoint = null},
                new() { Id = Guid.NewGuid(), Name = "Samsung Galaxy S23", Sku = "SAM23", Stock = 10, ComputedReorderPoint = 15, ManualReorderPoint = 25 },
                new() { Id = Guid.NewGuid(), Name = "Google Pixel 9", Sku = "PIX9", Stock = 30, ComputedReorderPoint = 12, ManualReorderPoint = null },
                new() { Id = Guid.NewGuid(), Name = "Dell XPS 13", Sku = "DX13", Stock = 8, ComputedReorderPoint = 10 },
                new() { Id = Guid.NewGuid(), Name = "MacBook Air M3", Sku = "MBA3", Stock = 20, ComputedReorderPoint = 10},
                new() { Id = Guid.NewGuid(), Name = "iPad Pro", Sku = "IPDPR", Stock = 15, ComputedReorderPoint = 8},
                new() { Id = Guid.NewGuid(), Name = "Sony WH-1000XM5", Sku = "SONYXM5", Stock = 40, ComputedReorderPoint = 10 },
                new() { Id = Guid.NewGuid(), Name = "Logitech MX Master 3", Sku = "LOGIMX3", Stock = 60, ComputedReorderPoint = 20},
                new() { Id = Guid.NewGuid(), Name = "Amazon Echo Dot 5th Gen", Sku = "ECHO5", Stock = 12, ComputedReorderPoint = 10},
                new() { Id = Guid.NewGuid(), Name = "Asus ROG Laptop", Sku = "ASUSROG", Stock = 6, ComputedReorderPoint = 8,},
                new() { Id = Guid.NewGuid(), Name = "HP Envy x360", Sku = "HPX360", Stock = 18, ComputedReorderPoint = 10},
                new() { Id = Guid.NewGuid(), Name = "Apple Watch Ultra", Sku = "AWU1", Stock = 14, ComputedReorderPoint = 6},
                new() { Id = Guid.NewGuid(), Name = "Fitbit Charge 6", Sku = "FIT6", Stock = 22, ComputedReorderPoint = 10 },
                new() { Id = Guid.NewGuid(), Name = "Canon EOS R8", Sku = "CANR8", Stock = 7, ComputedReorderPoint = 5},
                new() { Id = Guid.NewGuid(), Name = "GoPro Hero 12", Sku = "GPH12", Stock = 9, ComputedReorderPoint = 6 }
            };

            await db.Items.AddRangeAsync(items);
            await db.SaveChangesAsync();

            // Create demand stats for each item (last 15 days)
            var stats = new List<DemandStat>();
            var rand = new Random();
            foreach (var item in items)
            {
                for (int i = 0; i < 15; i++)
                {
                    stats.Add(new DemandStat
                    {
                        ItemId = item.Id,
                        Day = DateTime.UtcNow.AddDays(-i),
                        Quantity = rand.Next(1, 10),
                    });
                }
            }
            await db.DemandStats.AddRangeAsync(stats);
            await db.SaveChangesAsync();

            // Add supplier orders
            var orders = new List<SupplierOrder>();
            int counter = 0;
            foreach (var item in items.Take(15))
            {
                orders.Add(new SupplierOrder
                {
                    Id = Guid.NewGuid(),
                    ItemId = item.Id,
                    Quantity = rand.Next(10, 100),
                    RequestedDeliveryDate = DateTime.UtcNow.AddDays(rand.Next(2, 10)),
                    Status =  counter++ % 3 == 0 ? OrderStatus.Confirmed : OrderStatus.Pending,
                });
            }
            // await db.SupplierOrders.AddRangeAsync(orders);
            // await db.SaveChangesAsync();
        }
    }
}
