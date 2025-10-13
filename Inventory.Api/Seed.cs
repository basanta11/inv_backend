using Inventory.Infrastructure;
using Inventory.Domain;

public static class Seed {
    public static async Task EnsureAsync(IApplicationBuilder app){
        using var s = app.ApplicationServices.CreateScope();
        var db = s.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!db.Items.Any()){
            db.Items.AddRange(
              new Item{ Sku="SKU-001", Name="Widget A", Stock=12, SafetyStock=5, LeadTimeDays=2 },
              new Item{ Sku="SKU-002", Name="Widget B", Stock=3,  SafetyStock=6, LeadTimeDays=4 }
            );
            await db.SaveChangesAsync();
        }
    }
}