using Inventory.Domain;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Inventory.App;

public class InventoryMonitor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPubSub _bus;

    public InventoryMonitor(IServiceScopeFactory scopeFactory, IPubSub bus)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var items = await db.Items.AsNoTracking().ToListAsync(ct);
            foreach (var i in items)
            {
                var thr = i.ManualReorderPoint ?? i.ComputedReorderPoint;
                if (thr > 0 && i.Stock < thr)
                    await _bus.PublishAsync(new StockLowEvent(i.Id, i.Stock, thr), ct);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }
    }
}