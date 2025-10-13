namespace Inventory.Domain;

public enum OrderStatus { Pending, Confirmed, Failed }

public class Item {
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sku { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Stock { get; set; }
    public int LeadTimeDays { get; set; } = 3;
    public int SafetyStock { get; set; } = 5;
    public int? ManualReorderPoint { get; set; }
    public int ComputedReorderPoint { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DemandStat {
    public long Id { get; set; }
    public Guid ItemId { get; set; }
    public DateTime Day { get; set; }
    public int Quantity { get; set; }
    public Item Item { get; set; } = default!;
}

public class SupplierOrder {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public int Quantity { get; set; }
    public DateTime RequestedDeliveryDate { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? SupplierRef { get; set; }
    public Item Item { get; set; } = default!;
}

public record StockLowEvent(Guid ItemId, int Stock, int ReorderPoint);

public interface IPubSub {
    Task PublishAsync<T>(T message, CancellationToken ct = default);
    void Subscribe<T>(Func<T, Task> handler);
}
