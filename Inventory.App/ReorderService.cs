using Inventory.Domain;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Inventory.App;

public interface IReorderService {
    Task<int> ComputeReorderPoint(Guid itemId, CancellationToken ct);
    Task RecomputeAllAsync(CancellationToken ct);
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
}