using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CẤU HÌNH DATABASE CONTEXT ---
builder.Services.AddDbContext<WarehouseDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("WarehouseDb")));

// 🌟 BỔ SUNG CHÍNH XÁC: Đăng ký dịch vụ CORS cho phép cổng ứng dụng Web Blazor kết nối
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5193", "https://localhost:5193", "https://localhost:7193")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- 2. ĐĂNG KÝ DỊCH VỤ XÁC THỰC JWT BEARER CHO .NET 10 ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"] ?? "Chuoi_Khoa_Bao_Mat_Sieu_Cap_Cua_Kho_Vat_Tu_2026_Dotnet10";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "WarehouseManagementApi",
        ValidAudience = jwtSettings["Audience"] ?? "WarehouseManagementClient",
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ClockSkew = TimeSpan.Zero,

        NameClaimType = "sub",
        RoleClaimType = "role"
    };
});

// Đăng ký dịch vụ ủy quyền (Authorization)
builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors(); //

// --- 3. ĐĂNG KÝ MIDDLEWARE BẢO MẬT (Đặt trước các Endpoint định tuyến) ---
//app.UseHttpsRedirection();

app.UseAuthentication(); // Kích hoạt giải mã Token danh tính người dùng
app.UseAuthorization();  // Kích hoạt kiểm tra quyền hạn truy cập

if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/database-check", async (WarehouseDbContext db) =>
    {
        try
        {
            var databaseName = await db.Database
                .SqlQueryRaw<string>(
                    "SELECT CAST(DB_NAME() AS nvarchar(128)) AS [Value]")
                .SingleAsync();

            return Results.Ok(new
            {
                status = "Connected",
                database = databaseName
            });
        }
        catch (Exception)
        {
            return Results.Problem(
                "Database connection failed. Check SQL Server and the development connection string.");
        }
    });
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapAuthEndpoints(app.Configuration);

// --- 4. MAP TOÀN BỘ ENDPOINT HỆ THỐNG ---
// Đăng ký mới Endpoint xử lý Authentication (Đăng nhập, kiểm tra IsActive, sinh JWT)

// Các endpoint nghiệp vụ kho hiện có
app.MapMaterialEndpoints();
app.MapWarehouseEndpoints();
app.MapStockTransactionEndpoints();
app.MapInventoryEndpoints();
app.MapStockTransactionHistoryEndpoints();
app.MapStockSummaryReportEndpoints();
app.MapStockSummaryExcelEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
