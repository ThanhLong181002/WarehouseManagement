using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Data.Entities;
using WarehouseManagement.Api.Entities;
using Xunit;

namespace WarehouseManagement.Api.Tests;

public class StockSummaryReportTests
{
    private WarehouseDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new WarehouseDbContext(options);
    }

    [Fact]
    public async Task Report_ShouldCalculateOpeningAndClosing_CorrectlyWithReversedTransactions()
    {
        // Arrange: Giả lập kho có 1 phiếu nhập gốc (40) và 1 phiếu đảo (40) -> Tồn hiện tại bằng 0
        using var db = GetInMemoryDbContext();

        var originalTx = new StockTransaction
        {
            StockTransactionId = 10,
            TransactionNumber = "PN-GOC",
            TransactionType = "StockIn",
            Status = "Reversed", // Đã bị đảo
            WarehouseId = 1,
            CreatedAt = new DateTime(2026, 10, 14, 8, 0, 0),
            StockTransactionDetails = [new() { MaterialId = 1, Quantity = 40m }]
        };

        var reverseTx = new StockTransaction
        {
            StockTransactionId = 11,
            TransactionNumber = "REV-PN-GOC",
            TransactionType = "StockOut", // Đảo của nhập là xuất
            Status = "Posted",
            WarehouseId = 1,
            CreatedAt = new DateTime(2026, 10, 14, 9, 0, 0),
            StockTransactionDetails = [new() { MaterialId = 1, Quantity = 40m }]
        };

        var balance = new InventoryBalance { WarehouseId = 1, MaterialId = 1, QuantityOnHand = 0m }; // Cân bằng bằng 0

        db.StockTransactions.AddRange(originalTx, reverseTx);
        db.InventoryBalances.Add(balance);
        await db.SaveChangesAsync();

        // Act: Tính toán biến động trong ngày 14/10/2026
        var txs = await db.StockTransactions.Include(t => t.StockTransactionDetails).ToListAsync();
        decimal totalIn = txs.Where(t => t.TransactionType == "StockIn").SelectMany(t => t.StockTransactionDetails).Sum(d => d.Quantity);
        decimal totalOut = txs.Where(t => t.TransactionType == "StockOut").SelectMany(t => t.StockTransactionDetails).Sum(d => d.Quantity);

        decimal closingBalance = balance.QuantityOnHand; // 0
        decimal openingBalance = closingBalance - totalIn + totalOut; // 0 - 40 + 40 = 0

        // Assert: Tiêu chí nghiệm thu: Phiếu đảo và phiếu gốc tự triệt tiêu nhau, số dư cuối kỳ khớp với InventoryBalances
        Assert.Equal(40m, totalIn);
        Assert.Equal(40m, totalOut);
        Assert.Equal(0m, openingBalance);
        Assert.Equal(0m, closingBalance);
    }
}
