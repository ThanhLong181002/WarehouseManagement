
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Api.Contracts;

public sealed class CreateWarehouseRequest
{
    [Required]
    [StringLength(50)]
    public string WarehouseCode { get; init; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string WarehouseName { get; init; } = string.Empty;
}
