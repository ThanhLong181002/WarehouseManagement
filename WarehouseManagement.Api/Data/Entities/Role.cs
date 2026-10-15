using System.Collections.Generic;

namespace WarehouseManagement.Api.Data.Entities;

public class Role
{
    public int RoleId { get; set; }

    // Đổi thành RoleName để khớp tuyệt đối với DB thực tế của bạn
    public required string RoleName { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
