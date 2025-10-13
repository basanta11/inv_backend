using Inventory.Domain;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Inventory.App;

public interface IReorderService {
    Task<int> ComputeReorderPoint(Guid itemId, CancellationToken ct);
    Task RecomputeAllAsync(CancellationToken ct);
    Task HandleStockLowAsync(StockLowEvent evt, CancellationToken ct);
}

public class ReorderService : IReorderService {
    private readonly AppDbContext _db;
    public ReorderService(AppDbContext db) => _db = db;

    public async Task<int> ComputeReorderPoint(Guid itemId, CancellationToken ct) {
        var it = await _db.Items.FindAsync(new object?[] { itemId }, ct) 
                 ?? throw new InvalidOperationException("Item not found");
        var since = DateTime.UtcNow.Date.AddDays(-30);
        var avg = await _db.DemandStats
            .Where(d => d.ItemId == itemId && d.Day >= since)
            .AverageAsync(d => (double?)d.Quantity, ct) ?? 0.0;

        var rop = (int)Math.Ceiling(avg * it.LeadTimeDays) + it.SafetyStock;
        it.ComputedReorderPoint = rop;
        await _db.SaveChangesAsync(ct);
        return rop;
    }

    public async Task RecomputeAllAsync(CancellationToken ct) {
        var ids = await _db.Items.Select(x => x.Id).ToListAsync(ct);
        foreach (var id in ids) await ComputeReorderPoint(id, ct);
    }
    public async Task HandleStockLowAsync(StockLowEvent evt, CancellationToken ct)
    {
        var item = await _db.Items.FindAsync(new object?[] { evt.ItemId }, ct);
        if (item is null) return;

        // avoid duplicate pending orders
        var hasPending = await _db.SupplierOrders
            .AnyAsync(o => o.ItemId == item.Id && o.Status == 0, ct);
        if (hasPending) return;

        // simple quantity heuristic
        var deficit = Math.Max(evt.ReorderPoint - evt.Stock, 0);
        var qty = Math.Max(deficit, 10);

        _db.SupplierOrders.Add(new SupplierOrder
        {
            ItemId = item.Id,
            Quantity = qty,
            Status = 0, // 0 = Pending
            // CreatedAt = DateTime.UztcNow
        });

        await _db.SaveChangesAsync(ct);
        Console.WriteLine($"[REORDER] Supplier order created for {item.Name} (qty {qty}).");
    }
}