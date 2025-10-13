using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.Infrastructure;
using Inventory.Domain;
using Inventory.App;
public class SetThresholdPayload
{
    public int ReorderPoint { get; set; }
    public int SafetyStock { get; set; }
}

[ApiController]
[Route("api/items")]
public class ItemsController : ControllerBase {
    private readonly AppDbContext _db; private readonly IPubSub _bus; private readonly IReorderService _reorder;
    public ItemsController(AppDbContext db, IPubSub bus, IReorderService reorder){ _db=db; _bus=bus; _reorder=reorder; }

    [HttpPost]
    public async Task<ActionResult<Item>> Add([FromBody] Item i, CancellationToken ct){
        _db.Items.Add(i); await _db.SaveChangesAsync(ct);
        await _reorder.ComputeReorderPoint(i.Id, ct);
        return CreatedAtAction(nameof(Get), new { id=i.Id }, i);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Item dto, CancellationToken ct){
        var i = await _db.Items.FindAsync(new object?[]{id}, ct);
        if(i==null) return NotFound();
        i.Sku=dto.Sku; i.Name=dto.Name; i.Stock=dto.Stock; i.LeadTimeDays=dto.LeadTimeDays; i.SafetyStock=dto.SafetyStock;
        await _db.SaveChangesAsync(ct);
        await _reorder.ComputeReorderPoint(i.Id, ct);
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, CancellationToken ct){
        var query=_db.Items.AsNoTracking();
        if(!string.IsNullOrWhiteSpace(q)) query=query.Where(x=>x.Name.Contains(q));
        var data = await query.OrderBy(x=>x.Name).ToListAsync(ct);
        return Ok(data.Select(x=> new {
            x.Id,x.Sku,x.Name,x.Stock,
            ReorderPoint = x.ManualReorderPoint ?? x.ComputedReorderPoint,
            Status = (x.ManualReorderPoint ?? x.ComputedReorderPoint) > 0 && x.Stock < (x.ManualReorderPoint ?? x.ComputedReorderPoint) ? "Low":"OK"
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Item>> Get(Guid id, CancellationToken ct){
        var it = await _db.Items.FindAsync(new object?[]{id}, ct);
        return it is null ? NotFound() : it;
    }

    [HttpPatch("{id:guid}/reorder-threshold")]
  public async Task<IActionResult> SetManual(Guid id, [FromBody] SetThresholdPayload payload, CancellationToken ct)
{
    var it = await _db.Items.FindAsync(new object?[] { id }, ct);
    if (it == null) return NotFound();

    it.ManualReorderPoint = payload.ReorderPoint;
    it.SafetyStock = payload.SafetyStock;

    await _db.SaveChangesAsync(ct);
    return NoContent();
}

    [HttpPatch("{id:guid}/force-reorder")]
    public async Task<IActionResult> ForceReorder(Guid id, [FromBody] int qty, CancellationToken ct){
        await _bus.PublishAsync(new StockLowEvent(id,0,0), ct);
        return Accepted();
    }

    [HttpGet("{id:guid}/demand")]
    public async Task<IActionResult> Demand30(Guid id, CancellationToken ct){
        var since=DateTime.UtcNow.Date.AddDays(-30);
        var rows=await _db.DemandStats.Where(d=>d.ItemId==id && d.Day>=since)
            .OrderBy(d=>d.Day).Select(d=>new{ d.Day, d.Quantity }).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPatch("{id:guid}/recompute")]
    public Task<int> Recompute(Guid id, CancellationToken ct)=>_reorder.ComputeReorderPoint(id, ct);
}
