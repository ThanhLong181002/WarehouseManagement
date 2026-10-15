
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Api.Contracts;

public sealed class CreateStockTransactionRequest
{
    [Required]
    [StringLength(20)]
    public string TransactionType { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int WarehouseId { get; init; }

    [Range(1, int.MaxValue)]
    public int CreatedByUserId { get; init; }

    [StringLength(500)]
    public string? Reason { get; init; }

    [Required]
    [MinLength(1)]
    public List<CreateStockTransactionDetailRequest> Details { get; init; }
        = new();
}

public sealed class CreateStockTransactionDetailRequest
{
    [Range(1, int.MaxValue)]
    public int MaterialId { get; init; }

    [Range(typeof(decimal), "0.001", "999999999999999.999")]
    public decimal Quantity { get; init; }
}
