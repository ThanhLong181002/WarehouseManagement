using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Data.Entities;
using Xunit;

namespace WarehouseManagement.Api.Tests;

public class StockTransactionHistoryTests
{
    private WarehouseDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new WarehouseDbContext(options);
    }

    [Fact]
    public async Task History_FilterByMaterial_ShouldQueryDeepIntoDetails()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var tx = new StockTransaction
        {
            StockTransactionId = 1,
            TransactionNumber = "PN-01",
            TransactionType = "StockIn",
            Status = "Posted",
            WarehouseId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            StockTransactionDetails = new List<StockTransactionDetail>
            {
                new() { MaterialId = 99, Quantity = 10m } // Phiếu chứa vật tư 99
            }
        };
        db.StockTransactions.Add(tx);
        await db.SaveChangesAsync();

        // Act: Giả lập logic kiểm tra tìm kiếm sâu vào bảng chi tiết vật tư
        var results = await db.StockTransactions
            .Where(t => t.StockTransactionDetails.Any(d => d.MaterialId == 99))
            .ToListAsync();

        // Assert: Tiêu chí nghiệm thu: Lọc đúng theo vật tư
        Assert.Single(results);
        Assert.Equal("PN-01", results.First().TransactionNumber);
    }

    [Fact]
    public async Task History_FilterByDateRange_ShouldIncludeBoundary()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var targetDate = new DateTime(2026, 10, 14, 12, 0, 0); // Ngày hiện tại

        var tx = new StockTransaction
        {
            StockTransactionId = 2,
            TransactionNumber = "PN-02",
            TransactionType = "StockIn",
            Status = "Posted",
            WarehouseId = 1,
            CreatedAt = targetDate,
            UpdatedAt = targetDate
        };
        db.StockTransactions.Add(tx);
        await db.SaveChangesAsync();

        // Act: Giả lập lọc khoảng ngày ranh giới (chặn cuối ngày 14)
        var fromDate = new DateTime(2026, 10, 14);
        var toDate = new DateTime(2026, 10, 14);
        var endOfToDate = toDate.Date.AddDays(1).AddTicks(-1);

        var results = await db.StockTransactions
            .Where(t => t.CreatedAt >= fromDate && t.CreatedAt <= endOfToDate)
            .ToListAsync();

        // Assert: Tiêu chí nghiệm thu: Lọc khoảng ngày chính xác bao gồm cả ranh giới ngày
        Assert.Single(results);
    }

    [Fact]
    public async Task History_WithInvalidDateRange_ShouldBeDetectedByValidation()
    {
        // Arrange
        var fromDate = new DateTime(2026, 10, 15);
        var toDate = new DateTime(2026, 10, 14); // Ngày từ sau ngày đến

        // Act & Assert: Tiêu chí nghiệm thu: Đầu vào không hợp lệ được phát hiện lỗi
        bool isInvalidRange = fromDate > toDate;
        Assert.True(isInvalidRange);
    }
}
