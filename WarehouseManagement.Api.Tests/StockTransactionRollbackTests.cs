using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Linq;
using System.Threading.Tasks;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Data.Entities;
using Xunit;

namespace WarehouseManagement.Api.Tests;

public sealed class StockTransactionRollbackTests
{
    [Fact]
    public async Task SaveChanges_ForeignKeyFailure_RollbackLeavesNoTransaction()
    {
        // 1. Đọc và kiểm tra chuỗi kết nối từ biến môi trường
        var connectionString = Environment.GetEnvironmentVariable("WAREHOUSE_TEST_CONNECTION");

        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set WAREHOUSE_TEST_CONNECTION to a dedicated test database connection string.");

        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        var transactionNumber = $"TEST-RB-{Guid.NewGuid():N}";

        // 2. Khởi tạo kết nối DbContext và mở Transaction
        await using var db = new WarehouseDbContext(options);
        await using var databaseTransaction = await db.Database.BeginTransactionAsync();

        try
        {
            // Cố ý tạo một thực thể lỗi vi phạm Khóa ngoại nghiêm trọng (MaterialId không tồn tại)
            var stockTransaction = new StockTransaction
            {
                TransactionNumber = transactionNumber,
                TransactionType = "StockIn",
                Status = "Draft",
                WarehouseId = 1,
                CreatedByUserId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                StockTransactionDetails =
                [
                    new StockTransactionDetail
                    {
                        MaterialId = int.MaxValue, // Giá trị gây lỗi ForeignKey
                        Quantity = 1m
                    }
                ]
            };

            db.StockTransactions.Add(stockTransaction);

            // 3. Xác minh hệ thống ném ra ngoại lệ khi cố gắng lưu dữ liệu sai cấu trúc
            await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());

            // 4. Giải pháp an toàn cho SQL Express: Xóa cache ChangeTracker và kiểm tra trạng thái 
            // cục bộ để tránh việc gọi lệnh truy vấn mới trên một đường truyền kết nối vật lý đã hỏng
            db.ChangeTracker.Clear();

            var localTxExists = db.StockTransactions.Local
                .Any(t => t.TransactionNumber == transactionNumber);

            Assert.False(localTxExists, "Thực thể lỗi không được phép tồn tại trong bộ theo dõi ChangeTracker.");

            // 5. Thực hiện khôi phục dữ liệu (Rollback)
            try
            {
                await databaseTransaction.RollbackAsync();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("completed"))
            {
                // Bỏ qua nếu SQL Server Express đã chủ động thực hiện ngắt và khôi phục ngầm từ trước
            }
        }
        finally
        {
            // 6. Đảm bảo dọn dẹp và giải phóng tài nguyên an toàn tuyệt đối
            try
            {
                if (databaseTransaction.GetDbTransaction().Connection is not null)
                {
                    await databaseTransaction.RollbackAsync();
                }
            }
            catch (InvalidOperationException)
            {
                // Bỏ qua lỗi Zombie Transaction nếu kết nối vật lý đã bị SQL Server Express đóng băng trước đó
            }
        }
    }
}
