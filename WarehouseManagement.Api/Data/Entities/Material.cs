using System;
using System.Collections.Generic;

namespace WarehouseManagement.Api.Data.Entities;

public partial class Material
{
    public int MaterialId { get; set; }

    public string MaterialCode { get; set; } = null!;

    public string MaterialName { get; set; } = null!;

    public string BaseUnit { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<StockTransactionDetail>
    StockTransactionDetails
    { get; set; } =
        new List<StockTransactionDetail>();
}
