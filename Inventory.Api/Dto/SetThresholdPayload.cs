namespace Inventory.Api.Dtos;

public class SetThresholdPayload
{
    public int? ManualReorderPoint { get; set; }
    public int? SafetyStock { get; set; }
}