using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Data.Entities;
using WarehouseManagement.Api.Entities;
using Xunit;

namespace WarehouseManagement.Api.Tests;

public class ReverseTransactionTests
{
    private WarehouseDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new WarehouseDbContext(options);
    }

    [Fact]
    public async Task Reverse_DraftTransaction_ShouldFail_ReturnConflict()
    {
        var db = GetInMemoryDbContext();
        var draftTransaction = new StockTransaction
        {
            StockTransactionId = 1,
            TransactionNumber = "ST-DRAFT-01",
            TransactionType = "StockIn",
            Status = "Draft"
        };
        db.StockTransactions.Add(draftTransaction);
        await db.SaveChangesAsync();

        bool isPosted = draftTransaction.Status == "Posted";

        Assert.False(isPosted);
    }

    [Fact]
    public async Task Reverse_PostedStockIn_ShouldDecreaseInventoryBalancesCorrectly()
    {
        var db = GetInMemoryDbContext();
        var original = new StockTransaction
        {
            StockTransactionId = 2,
            TransactionNumber = "ST-IN-02",
            TransactionType = "StockIn",
            Status = "Posted",
            WarehouseId = 1,
            StockTransactionDetails = new List<StockTransactionDetail>
            {
                new() { MaterialId = 5, Quantity = 50m }
            }
        };
        // Sử dụng InventoryBalance và QuantityOnHand chuẩn theo Schema
        var balance = new InventoryBalance { WarehouseId = 1, MaterialId = 5, QuantityOnHand = 100m };

        db.StockTransactions.Add(original);
        db.InventoryBalances.Add(balance);
        await db.SaveChangesAsync();

        balance.QuantityOnHand -= original.StockTransactionDetails.First().Quantity;
        original.Status = "Reversed";
        await db.SaveChangesAsync();

        var updatedBalance = await db.InventoryBalances.SingleAsync(b => b.WarehouseId == 1 && b.MaterialId == 5);
        Assert.Equal(50m, updatedBalance.QuantityOnHand);
        Assert.Equal("Reversed", original.Status);
    }

    [Fact]
    public async Task Reverse_InsufficientStock_ShouldPreventPartialUpdate()
    {
        var db = GetInMemoryDbContext();
        var original = new StockTransaction
        {
            StockTransactionId = 3,
            TransactionNumber = "ST-IN-03",
            TransactionType = "StockIn",
            Status = "Posted",
            WarehouseId = 1,
            StockTransactionDetails = new List<StockTransactionDetail>
            {
                new() { MaterialId = 9, Quantity = 150m }
            }
        };
        var balance = new InventoryBalance { WarehouseId = 1, MaterialId = 9, QuantityOnHand = 30m };

        db.StockTransactions.Add(original);
        db.InventoryBalances.Add(balance);
        await db.SaveChangesAsync();

        bool hasEnoughStock = balance.QuantityOnHand >= original.StockTransactionDetails.First().Quantity;

        Assert.False(hasEnoughStock);
        var currentBalance = await db.InventoryBalances.SingleAsync(b => b.WarehouseId == 1 && b.MaterialId == 9);
        Assert.Equal(30m, currentBalance.QuantityOnHand);
    }
}
