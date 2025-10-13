using Microsoft.AspNetCore.Mvc;
using Inventory.Infrastructure;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/supplier")]
public class SupplierController : ControllerBase {
    private readonly AppDbContext _db; private readonly IHttpClientFactory _http;
    public SupplierController(AppDbContext db, IHttpClientFactory http){ _db=db; _http=http; }

    [HttpGet]
    public async Task<IActionResult> GetSupplierOrders([FromQuery] string? status = null, CancellationToken ct = default)
    {
        var query = _db.SupplierOrders
            .Include(o => o.Item)
            .AsNoTracking()
            .AsQueryable();

        // if (!string.IsNullOrEmpty(status))
        //     query = query.Where(o => o.Status == status);

        var orders = await query
            // .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return Ok(orders);
    }

    public record PlaceOrderDto(Guid ItemId, int Quantity, DateTime DeliveryDate);

    [HttpPost("place-order")]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto dto, CancellationToken ct){
        var item = await _db.Items.FindAsync(new object?[]{dto.ItemId}, ct);
        if(item==null) return NotFound();
        var order = new SupplierOrder{ ItemId=item.Id, Quantity=dto.Quantity, RequestedDeliveryDate=dto.DeliveryDate };
        _db.SupplierOrders.Add(order); await _db.SaveChangesAsync(ct);

        _ = Task.Run(async ()=>{
            await Task.Delay(1500, ct);
            var client = _http.CreateClient();
            await client.PostAsJsonAsync("http://localhost:5000/webhook/order-confirmation",
                new { orderId = order.Id, status="Confirmed", supplierRef=$"SUP-{Random.Shared.Next(10000,99999)}" }, ct);
        }, ct);

        return Accepted(new { order.Id, order.Status });
    }
}