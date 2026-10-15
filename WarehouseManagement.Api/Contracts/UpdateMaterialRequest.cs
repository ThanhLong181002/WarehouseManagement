
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Api.Contracts;

public sealed class UpdateMaterialRequest
{
    [Required]
    [StringLength(200)]
    public string MaterialName { get; init; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string BaseUnit { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
