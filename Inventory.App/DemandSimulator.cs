using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Inventory.App;

public class DemandSimulator : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DemandSimulator(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await EnsureToday(ct);
        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var next = now.Date.AddDays(1).AddMinutes(5);
                await Task.Delay(next - now, ct);
                await EnsureToday(ct);
            }
        }, ct);
    }

    private async Task EnsureToday(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var today = DateTime.UtcNow.Date;
        var items = await db.Items.ToListAsync(ct);
        foreach (var it in items)
        {
            var row = await db.DemandStats.FirstOrDefaultAsync(d => d.ItemId == it.Id && d.Day == today, ct);
            if (row == null) db.DemandStats.Add(new() { ItemId = it.Id, Day = today, Quantity = Random.Shared.Next(0, 5) });
            else row.Quantity += Random.Shared.Next(0, 3);
        }
        await db.SaveChangesAsync(ct);
    }
}