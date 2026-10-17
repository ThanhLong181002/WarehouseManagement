using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Data.Entities;
using WarehouseManagement.Api.Entities; // ✅ ĐÃ BỔ SUNG: Khắc phục lỗi gạch đỏ User và UserRole

namespace WarehouseManagement.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        // 🌟 1. ENDPOINT ĐĂNG NHẬP HỆ THỐNG: POST /api/auth/login
        app.MapPost("/api/auth/login", async (LoginRequest request, WarehouseDbContext db, IConfiguration config) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { message = "Tài khoản và mật khẩu không được để trống." });
            }

            var user = await db.Users
                .FirstOrDefaultAsync(u => u.Username.Trim().ToLower() == request.Username.Trim().ToLower());

            if (user == null)
            {
                return Results.BadRequest(new { message = "Tài khoản hoặc mật khẩu không chính xác." });
            }

            if (!user.IsActive)
            {
                return Results.BadRequest(new { message = "Tài khoản này hiện đang bị khóa khỏi hệ thống." });
            }

            bool isPasswordValid = false;
            try
            {
                var passwordHasher = new PasswordHasher<object>();
                var verificationResult = passwordHasher.VerifyHashedPassword(new object(), user.PasswordHash, request.Password);
                isPasswordValid = (verificationResult != PasswordVerificationResult.Failed);
            }
            catch (FormatException)
            {
                isPasswordValid = (user.PasswordHash.Trim() == request.Password.Trim());
            }

            if (!isPasswordValid)
            {
                return Results.BadRequest(new { message = "Tài khoản hoặc mật khẩu không chính xác." });
            }

            var userRole = await db.UserRoles
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.UserId == user.UserId);

            if (userRole == null || userRole.Role == null)
            {
                return Results.BadRequest(new { message = "Tài khoản chưa được phân vai trò trên hệ thống." });
            }

            var claims = new List<Claim>
            {
                new Claim("sub", user.Username),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("FullName", user.FullName),
                new Claim("role", userRole.Role.RoleName),
                new Claim(ClaimTypes.Role, userRole.Role.RoleName)
            };

            var jwtSettings = config.GetSection("Jwt");
            var jwtKey = jwtSettings["Key"] ?? "Chuoi_Khoa_Bao_Mat_Sieu_Cap_Cua_Kho_Vat_Tu_2026_Dotnet10";
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
            var symmetricKey = new SymmetricSecurityKey(keyBytes);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims, "Jwt", "sub", "role"),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = jwtSettings["Issuer"] ?? "WarehouseManagementApi",
                Audience = jwtSettings["Audience"] ?? "WarehouseManagementClient",
                SigningCredentials = new SigningCredentials(symmetricKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtTokenString = tokenHandler.WriteToken(token);

            return Results.Ok(new
            {
                token = jwtTokenString,
                username = user.Username,
                role = userRole.Role.RoleName
            });
        });

        // 🌟 2. ENDPOINT XUẤT DANH SÁCH NGƯỜI DÙNG: GET /api/users
        app.MapGet("/api/users", async (WarehouseDbContext db, int page = 1, int pageSize = 20) =>
        {
            var query = db.Users.AsNoTracking();

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var rawItems = await query
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = rawItems.Select(u => new
            {
                Id = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                Email = "Không có thông tin",
                Role = u.UserRoles.Select(ur => ur.Role.RoleName).FirstOrDefault() ?? "Staff",
                IsActive = u.IsActive,
                LastLogin = (DateTime?)null
            }).ToList();

            return Results.Ok(new
            {
                page,
                pageSize,
                totalCount,
                totalPages,
                items
            });
        });

        // 🌟 3. ENDPOINT TẠO NGƯỜI DÙNG MỚI: POST /api/users
        app.MapPost("/api/users", async (CreateUserRequest request, WarehouseDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.FullName))
            {
                return Results.BadRequest(new { message = "Vui lòng nhập đầy đủ Tài khoản, Mật khẩu và Họ tên." });
            }

            var isUsernameTaken = await db.Users.AnyAsync(u => u.Username.ToLower() == request.Username.Trim().ToLower());
            if (isUsernameTaken)
            {
                return Results.BadRequest(new { message = "Tên tài khoản này đã tồn tại trên hệ thống." });
            }

            var passwordHasher = new PasswordHasher<object>();
            var hashedPassword = passwordHasher.HashPassword(new object(), request.Password.Trim());

            var newUser = new User
            {
                Username = request.Username.Trim(),
                PasswordHash = hashedPassword,
                FullName = request.FullName.Trim(),
                IsActive = true
            };

            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            var role = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == request.Role);
            if (role != null)
            {
                var userRole = new UserRole
                {
                    UserId = newUser.UserId,
                    RoleId = role.RoleId
                };
                db.UserRoles.Add(userRole);
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { message = "Tạo tài khoản người dùng thành công!" });
        });

        // 🌟 4. ENDPOINT CẬP NHẬT NGƯỜI DÙNG: PUT /api/users/{id}
        app.MapPut("/api/users/{id:int}", async (int id, UpdateUserRequest request, WarehouseDbContext db) =>
        {
            // 1. Tìm người dùng trong Database
            var user = await db.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return Results.NotFound(new { message = "Không tìm thấy tài khoản người dùng." });
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return Results.BadRequest(new { message = "Họ và tên không được để trống." });
            }

            // 2. Cập nhật thông tin cơ bản
            user.FullName = request.FullName.Trim();
            user.IsActive = request.IsActive;

            // 3. Cập nhật mật khẩu mới nếu Frontend có nhập đổi mật khẩu
            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                var passwordHasher = new PasswordHasher<object>();
                user.PasswordHash = passwordHasher.HashPassword(new object(), request.NewPassword.Trim());
            }

            // 4. Cập nhật lại vai trò (Role)
            var currentRole = user.UserRoles.FirstOrDefault();
            var newRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == request.Role);

            if (newRole != null)
            {
                if (currentRole != null)
                {
                    // Nếu đã có quyền cũ -> xóa quyền cũ đi
                    db.UserRoles.Remove(currentRole);
                }

                // Thêm quyền mới gán vào tài khoản
                var newUserRole = new UserRole
                {
                    UserId = user.UserId,
                    RoleId = newRole.RoleId
                };
                db.UserRoles.Add(newUserRole);
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Cập nhật thông tin tài khoản thành công!" });
        });

        return app;
    }
}

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Staff";
}

// DTO Request nhận dữ liệu cập nhật từ Frontend
public sealed class UpdateUserRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Staff";
    public bool IsActive { get; set; } = true;
    public string? NewPassword { get; set; } // Hỗ trợ reset mật khẩu nếu cần
}