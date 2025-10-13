using Microsoft.AspNetCore.Mvc;
using Inventory.Infrastructure;
using Inventory.Domain;

[ApiController]
[Route("webhook")]
public class WebhooksController : ControllerBase
{
    private readonly AppDbContext _db;
    public WebhooksController(AppDbContext db) => _db = db;

    public record ConfirmationDto(Guid orderId, string status, string? supplierRef);

    [HttpPost("order-confirmation")]
    public async Task<IActionResult> OrderConfirmation([FromBody] ConfirmationDto dto, CancellationToken ct)
    {
        var order = await _db.SupplierOrders.FindAsync(new object?[] { dto.orderId }, ct);
        if (order == null) return NotFound();
        order.Status = dto.status.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) ? OrderStatus.Confirmed : OrderStatus.Failed;
        order.SupplierRef = dto.supplierRef;

        if (order.Status == OrderStatus.Confirmed)
        {
            var item = await _db.Items.FindAsync(new object?[] { order.ItemId }, ct);
            if (item != null) item.Stock += order.Quantity;
        }
        await _db.SaveChangesAsync(ct);
        return Ok(new { order.Id, order.Status });
    }
}