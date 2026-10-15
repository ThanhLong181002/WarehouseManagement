
using WarehouseManagement.Api.Data.Entities;

namespace WarehouseManagement.Api.Entities;

public class InventoryBalance
{
    public int MaterialId { get; set; }

    public int WarehouseId { get; set; }

    public decimal QuantityOnHand { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Material Material { get; set; } = null!;

    public Warehouse Warehouse { get; set; } = null!;
}
