using System;
using System.Collections.Generic;

namespace WarehouseManagement.Api.Data.Entities;

public class User
{
    // Ánh xạ đúng với cột UserId (PK, int, not null) trong DB
    public int UserId { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string FullName { get; set; }
    public bool IsActive { get; set; }

    // Ánh xạ đúng với cột CreatedAt (datetime2(0), not null)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
