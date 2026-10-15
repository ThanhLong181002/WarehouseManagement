using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Data.Entities;
using WarehouseManagement.Api.Entities;
using Xunit;
using BC = BCrypt.Net.BCrypt;

namespace WarehouseManagement.Api.Tests;

public class SystemSecurityTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client;
    private readonly WarehouseDbContext _db;
    private readonly IServiceScope _scope;

    public SystemSecurityTests(WebApplicationFactory<Program> factory)
    {
        // Khởi tạo tên DB ngẫu nhiên bằng Guid để cô lập hoàn toàn giữa các luồng test song song
        string isolatedDbName = $"Sec_Real_Db_{Guid.NewGuid()}";

        var webWithMock = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("https_port", "5001");
            builder.ConfigureServices(services =>
            {
                services.Replace(ServiceDescriptor.Scoped<DbContextOptions<WarehouseDbContext>>(_ =>
                    new DbContextOptionsBuilder<WarehouseDbContext>().UseInMemoryDatabase(isolatedDbName).Options));

                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opt =>
                {
                    // Xóa bỏ bản đồ ánh xạ dài để nhận diện chính xác nhãn "role" thô
                    System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        ValidateIssuerSigningKey = false,
                        RoleClaimType = "role",
                        NameClaimType = "sub",
                        SignatureValidator = (t, _) => new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(t)
                    };
                });
            });
        });
        _client = webWithMock.CreateClient();
        _scope = webWithMock.Services.CreateScope();
        _db = _scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        _db.Database.EnsureCreated();

        // Seed dữ liệu bảo mật (Mật khẩu băm BCrypt, cấu hình phân hệ quyền)
        _db.Roles.AddRange(new Role { RoleId = 1, RoleName = "WarehouseKeeper" }, new Role { RoleId = 2, RoleName = "Guest" });
        _db.Users.AddRange(
            new User { UserId = 1, Username = "keeper", PasswordHash = BC.HashPassword("Pass123"), FullName = "Thủ kho", IsActive = true, CreatedAt = DateTime.UtcNow },
            new User { UserId = 2, Username = "locked", PasswordHash = BC.HashPassword("Pass123"), FullName = "Khóa", IsActive = false, CreatedAt = DateTime.UtcNow },
            new User { UserId = 3, Username = "guest", PasswordHash = BC.HashPassword("Pass123"), FullName = "Khách", IsActive = true, CreatedAt = DateTime.UtcNow });
        _db.UserRoles.AddRange(new UserRole { UserId = 1, RoleId = 1 }, new UserRole { UserId = 3, RoleId = 2 });
        _db.InventoryBalances.Add(new InventoryBalance { WarehouseId = 1, MaterialId = 1, QuantityOnHand = 500m, UpdatedAt = DateTime.UtcNow });
        _db.SaveChanges();
    }

    private async Task<string> GetToken(string user, string pass)
    {
        var res = await _client.PostAsJsonAsync("/api/auth/login", new { Username = user, Password = pass });
        if (res.StatusCode != HttpStatusCode.OK) return string.Empty;
        var data = await res.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return data?["token"] ?? string.Empty;
    }

    [Fact]
    public async Task SEC_01_02_08_AuthenticationLuong()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/stock-transactions")).StatusCode);
        var reqFake = new HttpRequestMessage(HttpMethod.Get, "/api/stock-transactions");
        reqFake.Headers.Authorization = new("Bearer", "FakeToken");
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(reqFake)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/auth/login", new { Username = "keeper", Password = "Wrong" })).StatusCode);
    }

    [Fact]
    public async Task SEC_03_04_09_AuthorizationAndStatusLuong()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/auth/login", new { Username = "locked", Password = "Pass123" })).StatusCode);

        var reqRole = new HttpRequestMessage(HttpMethod.Get, "/api/stock-transactions");
        reqRole.Headers.Authorization = new("Bearer", await GetToken("guest", "Pass123"));
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(reqRole)).StatusCode);

        var reqOk = new HttpRequestMessage(HttpMethod.Get, "/api/stock-transactions");
        reqOk.Headers.Authorization = new("Bearer", await GetToken("keeper", "Pass123"));
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(reqOk)).StatusCode);
    }

    [Fact]
    public async Task SEC_05_06_DataAndIdentityProtectionLuong()
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/stock-transactions") { Content = JsonContent.Create(new { WarehouseId = 1, TransactionType = "StockIn", CreatedByUserId = 999, Details = new[] { new { MaterialId = 1, Quantity = 10m } } }) };
        req.Headers.Authorization = new("Bearer", await GetToken("keeper", "Pass123"));
        Assert.Equal(HttpStatusCode.Created, (await _client.SendAsync(req)).StatusCode);
        Assert.Equal(1, (await _db.StockTransactions.OrderByDescending(t => t.StockTransactionId).FirstAsync()).CreatedByUserId);

        _db.StockTransactions.Add(new StockTransaction { StockTransactionId = 99, TransactionNumber = "PN01", TransactionType = "StockIn", Status = "Draft", WarehouseId = 1, CreatedByUserId = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        _db.StockTransactionDetails.Add(new StockTransactionDetail { StockTransactionId = 99, MaterialId = 1, Quantity = 100m });
        await _db.SaveChangesAsync();

        var reqPost = new HttpRequestMessage(HttpMethod.Post, "/api/stock-transactions/99/post");
        reqPost.Headers.Authorization = new("Bearer", await GetToken("guest", "Pass123"));
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(reqPost)).StatusCode);
        Assert.Equal(500m, (await _db.InventoryBalances.AsNoTracking().SingleAsync(b => b.WarehouseId == 1 && b.MaterialId == 1)).QuantityOnHand);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _client.Dispose();
    }
}
