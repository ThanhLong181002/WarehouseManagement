
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Api.Contracts;

public sealed class UpdateWarehouseRequest
{
    [Required]
    [StringLength(200)]
    public string WarehouseName { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
