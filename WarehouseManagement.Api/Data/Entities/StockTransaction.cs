using System;
using System.Collections.Generic;

namespace WarehouseManagement.Api.Data.Entities;

public partial class StockTransaction
{
    public int StockTransactionId { get; set; }

    public string TransactionNumber { get; set; } = null!;

    public string TransactionType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int WarehouseId { get; set; }

    public int CreatedByUserId { get; set; }

    public int? PostedByUserId { get; set; }

    public int? ReversesTransactionId { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? PostedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual StockTransaction? InverseReversesTransaction { get; set; }

    public virtual StockTransaction? ReversesTransaction { get; set; }

    public virtual ICollection<StockTransactionDetail>
    StockTransactionDetails
    { get; set; } =
        new List<StockTransactionDetail>();
}
