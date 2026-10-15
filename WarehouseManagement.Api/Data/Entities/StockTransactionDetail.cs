using System;


namespace WarehouseManagement.Api.Data.Entities;

public partial class StockTransactionDetail
{
    public int StockTransactionDetailId { get; set; }

    public int StockTransactionId { get; set; }

    public int MaterialId { get; set; }

    public decimal Quantity { get; set; }

    public virtual Material Material { get; set; } = null!;

    public virtual StockTransaction StockTransaction { get; set; } = null!;
}

