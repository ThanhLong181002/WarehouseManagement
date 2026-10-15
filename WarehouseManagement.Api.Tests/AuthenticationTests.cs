using System;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarehouseManagement.Api.Data;
using System.Linq;
using Xunit;

namespace WarehouseManagement.Api.Tests;

public class AuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthenticationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("https_port", "5001");

            builder.ConfigureServices(services =>
            {
                // 1. Tìm và xóa sạch TẤT CẢ các descriptor liên quan đến WarehouseDbContext cũ để tránh xung đột Provider
                var dbContextOptionsDescriptors = services
                    .Where(d => d.ServiceType.Namespace != null && d.ServiceType.Namespace.Contains("EntityFrameworkCore"))
                    .ToList();

                foreach (var descriptor in dbContextOptionsDescriptors)
                {
                    services.Remove(descriptor);
                }

                var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(WarehouseDbContext));
                if (contextDescriptor != null) services.Remove(contextDescriptor);

                var optionsDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<WarehouseDbContext>));
                if (optionsDescriptor != null) services.Remove(optionsDescriptor);

                // 2. Đăng ký MỚI hoàn toàn một Context độc lập sử dụng InMemory
                services.AddDbContext<WarehouseDbContext>(options =>
                {
                    options.UseInMemoryDatabase("AuthTest_Pure_Isolated_Db");
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Login_With_Wrong_Credentials_Should_Return_400_BadRequest()
    {
        // Act: Gửi yêu cầu đăng nhập với tài khoản không tồn tại
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Username = "invalid_user_2026",
            Password = "WrongPassword123"
        });

        // Assert: Tiêu chí nghiệm thu - Phải trả về lỗi 400 BadRequest có kiểm soát thay vì lỗi sập kết nối 500
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Access_Secured_Endpoint_Without_Token_Should_Return_401_Unauthorized()
    {
        // Act: Truy cập endpoint nghiệp vụ kho khi không có Token xác thực
        var response = await _client.PostAsJsonAsync("/api/stock-transactions", new
        {
            WarehouseId = 1,
            TransactionType = "StockIn",
            Details = new[] { new { MaterialId = 1, Quantity = 5.0m } }
        });

        // Assert: Tiêu chí nghiệm thu - Chặn đứng truy cập không hợp lệ
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
