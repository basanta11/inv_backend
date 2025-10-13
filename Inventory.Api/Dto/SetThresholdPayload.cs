namespace Inventory.Api.Dtos;

public class SetThresholdPayload
{
    public int? ReorderPoint { get; set; }
    public int? SafetyStock { get; set; }
}