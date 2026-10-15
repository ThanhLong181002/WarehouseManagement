using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WarehouseManagement.Api.Data;

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

            // Cơ chế xác thực mật khẩu thông minh chống lỗi Format Base-64
            bool isPasswordValid = false;
            try
            {
                var passwordHasher = new PasswordHasher<object>();
                var verificationResult = passwordHasher.VerifyHashedPassword(new object(), user.PasswordHash, request.Password);
                isPasswordValid = (verificationResult != PasswordVerificationResult.Failed);
            }
            catch (FormatException)
            {
                // Giải pháp dự phòng tự động chuyển sang so sánh chuỗi thuần nếu chuỗi DB chưa băm
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

            // Thiết lập danh sách Claims thông tin đa nền tảng (Đồng bộ chữ viết thường)
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
        // Áp dụng kiểm tra quyền linh hoạt không phân biệt hoa thường trực tiếp qua ClaimsPrincipal
        app.MapGet("/api/users", [Authorize] async (ClaimsPrincipal userPrincipal, WarehouseDbContext db, int page = 1, int pageSize = 20) =>
        {
            // Trích xuất giá trị claim "role" viết thường từ mã Token bảo mật gửi lên
            var userRole = userPrincipal.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

            // Kiểm tra phân quyền an toàn, chấp nhận mọi định dạng: Admin, admin, ADMIN...
            if (string.IsNullOrWhiteSpace(userRole) || !userRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Forbid(); // Trả về mã lỗi 403 an toàn nếu không phải quản trị viên
            }

            var query = db.Users.AsNoTracking();

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Nạp dữ liệu liên kết kèm bảng quyền hạn Roles để trích xuất RoleName
            var rawItems = await query
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Ánh xạ (Mapping) dữ liệu trả về cấu trúc DTO Frontend đang mong đợi
            var items = rawItems.Select(u => new
            {
                Id = u.UserId, // Khớp thuộc tính Id cục bộ phía Client
                Username = u.Username,
                FullName = u.FullName,
                Email = "Không có thông tin", // Gán chuỗi mặc định
                Role = u.UserRoles.Select(ur => ur.Role.RoleName).FirstOrDefault() ?? "Staff", // Trích xuất tên quyền hạn
                IsActive = u.IsActive,
                LastLogin = (DateTime?)null // Gán tạm null theo DTO
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

        return app;
    }
}

// DTO Request phục vụ riêng cho Endpoint đăng nhập hệ thống
public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
