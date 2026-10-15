using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using WarehouseManagement.Api.Data;
using Xunit;

namespace WarehouseManagement.Api.Tests;

public class AuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private const string ServerSecretKey = "Chuoi_Khoa_Bao_Mat_Sieu_Cap_Cua_Kho_Vat_Tu_2026_Dotnet10";

    public AuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("https_port", "5001");

            builder.ConfigureServices(services =>
            {
                // Cô lập cơ sở dữ liệu kiểm thử trong bộ nhớ RAM
                services.RemoveAll(typeof(DbContextOptions<WarehouseDbContext>));
                services.AddDbContext<WarehouseDbContext>(options => options.UseInMemoryDatabase("RoleTest_Db"));
            });
        }).CreateClient();
    }

    // Hàm sinh mã Token test đồng bộ 100% định dạng claim thô viết thường với Server thực tế
    private string GenerateTestToken(string userId, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(ServerSecretKey);

        // 🛑 GIẢI PHÁP GỐC RỄ: Sử dụng chuỗi thô viết thường "sub" và "role" thay vì hằng số URI của ClaimTypes
        var claims = new List<Claim>
        {
            new Claim("sub", userId),
            new Claim("role", role) // Quyền "Guest" không hợp lệ để ép hệ thống ném ra lỗi 403
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims, "Bearer"),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "WarehouseManagementApi",
            Audience = "WarehouseManagementClient",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task Request_Without_Token_Should_Return_401_Unauthorized()
    {
        // Act: Gọi API xem danh sách phiếu khi chưa đăng nhập (Thiếu hẳn Token)
        var response = await _client.GetAsync("/api/stock-transactions");

        // Assert: Tiêu chí nghiệm thu - Chặn vô danh và trả về đúng 401 Unauthorized
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_With_Invalid_Role_Should_Return_403_Forbidden()
    {
        // Arrange: Tạo token có chữ ký hợp lệ nhưng vai trò "Guest" (Sai vai trò nghiệp vụ kho của API)
        var token = GenerateTestToken("99", "Guest");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/stock-transactions");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act: Gửi request kiểm thử đến Server ảo
        var response = await _client.SendAsync(request);

        // Assert: Tiêu chí nghiệm thu - Danh tính đúng nhưng sai Role, hệ thống bắt buộc trả về đúng 403 Forbidden
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
