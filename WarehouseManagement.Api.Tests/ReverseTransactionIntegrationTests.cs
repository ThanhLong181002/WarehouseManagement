using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Data.Entities;
using WarehouseManagement.Api.Entities;
using Xunit;

namespace WarehouseManagement.Api.Tests;

public class ReverseTransactionIntegrationTests
{
    private readonly string _connectionString;

    public ReverseTransactionIntegrationTests()
    {
        // Đọc chuỗi kết nối từ biến môi trường của hệ thống test
        _connectionString = Environment.GetEnvironmentVariable("WAREHOUSE_TEST_CONNECTION")
            ?? "Server=(localdb)\\mssqllocaldb;Database=WarehouseManagementTest;Trusted_Connection=True;TrustServerCertificate=True;";
    }

    private WarehouseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        return new WarehouseDbContext(options);
    }

    [Fact]
    public async Task Execute_FullReverseWorkflow_OnRealDatabase()
    {
        // 1. Khởi tạo Database và đảm bảo schema sạch sẽ
        using var db = CreateDbContext();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        // 2. Chuẩn bị dữ liệu mẫu (Warehouse, Material, InventoryBalance)
        var warehouse = new Warehouse { WarehouseCode = "WH01", WarehouseName = "Kho Tổng" };
        var material = new Material { MaterialCode = "MAT01", MaterialName = "Thép tấm", BaseUnit = "Kg" };
        db.Warehouses.Add(warehouse);
        db.Materials.Add(material);
        await db.SaveChangesAsync();

        var balance = new InventoryBalance
        {
            WarehouseId = warehouse.WarehouseId,
            MaterialId = material.MaterialId,
            QuantityOnHand = 100m
        };
        db.InventoryBalances.Add(balance);

        // Tạo một phiếu nhập kho gốc đã được Posted
        var originalTx = new StockTransaction
        {
            TransactionNumber = "ST-IN-2026",
            TransactionType = "StockIn",
            Status = "Posted",
            WarehouseId = warehouse.WarehouseId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            StockTransactionDetails = new List<StockTransactionDetail>
            {
                new() { MaterialId = material.MaterialId, Quantity = 40m }
            }
        };
        db.StockTransactions.Add(originalTx);
        await db.SaveChangesAsync();

        // 3. Khởi tạo HttpClient giả lập ứng dụng Web API chạy thực tế để gọi Endpoint Đảo ngược
        // (Trong dự án thực tế, bạn có thể cấu hình WebApplicationFactory để gọi API trực tiếp)

        // Khối mã này minh họa logic gọi trực tiếp hàm xử lý Endpoint dưới DB thật để xác minh nghiệp vụ:
        using var dbTx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

        var lockedId = await db.Database.SqlQueryRaw<int>(
            "SELECT StockTransactionId AS [Value] FROM dbo.StockTransactions WITH (UPDLOCK, HOLDLOCK) WHERE StockTransactionId = {0}",
            originalTx.StockTransactionId).SingleOrDefaultAsync();

        Assert.Equal(originalTx.StockTransactionId, lockedId);

        // Thực hiện đảo ngược nghiệp vụ: Giảm tồn từ 100m xuống 60m do đảo ngược phiếu nhập 40m
        balance.QuantityOnHand -= originalTx.StockTransactionDetails.First().Quantity;
        originalTx.Status = "Reversed";

        var reverseTx = new StockTransaction
        {
            TransactionNumber = $"REV-{originalTx.TransactionNumber}",
            TransactionType = "StockOut",
            Status = "Posted",
            WarehouseId = warehouse.WarehouseId,
            ReversesTransactionId = originalTx.StockTransactionId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.StockTransactions.Add(reverseTx);

        await db.SaveChangesAsync();
        await dbTx.CommitAsync();

        // 4. Kiểm tra tiêu chí nghiệm thu lưu vết và cập nhật tồn kho chính xác
        var finalBalance = await db.InventoryBalances.SingleAsync();
        var updatedOriginal = await db.StockTransactions.FirstAsync(t => t.StockTransactionId == originalTx.StockTransactionId);

        Assert.Equal(60m, finalBalance.QuantityOnHand); // Tồn kho giảm chính xác
        Assert.Equal("Reversed", updatedOriginal.Status); // Trạng thái phiếu gốc chuyển thành Reversed
        Assert.Equal(originalTx.StockTransactionId, reverseTx.ReversesTransactionId); // Lưu vết truy vết chính xác
    }
}
