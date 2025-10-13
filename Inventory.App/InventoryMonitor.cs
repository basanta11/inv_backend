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
        // optional: guard against exceptions -> no tight loops
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(30)); // check every 30s
        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var items = await db.Items.AsNoTracking().ToListAsync(ct);
                Console.WriteLine($"[Monitor] Checked {items.Count} items at {DateTime.Now}");

                foreach (var i in items)
                {
                    var thr = i.ManualReorderPoint ?? i.ComputedReorderPoint;

                    if (thr > 0 && i.Stock < thr)
                    {
                        Console.WriteLine($"[Monitor] PUBLISH StockLowEvent for {i.Name} (stock={i.Stock}, reorder={thr})");
                        await _bus.PublishAsync(new StockLowEvent(i.Id, i.Stock, thr), ct);
                    }
                    else
                    {
                        Console.WriteLine($"[Monitor] Item {i.Name}: stock={i.Stock}, reorder={thr}");
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[Monitor] Error: {ex.Message}");
                // small backoff so we don't tight-loop on errors
                await Task.Delay(1000, ct);
            }
        }
    }
}

