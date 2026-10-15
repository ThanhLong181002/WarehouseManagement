
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Api.Contracts;

public sealed class CreateMaterialRequest
{
    [Required]
    [StringLength(50)]
    public string MaterialCode { get; init; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string MaterialName { get; init; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string BaseUnit { get; init; } = string.Empty;
}
