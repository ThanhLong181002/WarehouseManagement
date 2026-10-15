
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Api.Contracts;

public sealed class PostStockTransactionRequest
{
    [Range(1, int.MaxValue)]
    public int PostedByUserId { get; init; }
}
