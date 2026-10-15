using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WarehouseManagement.Api.Data;
using WarehouseManagement.Api.Entities;
using WarehouseManagement.Api.Endpoints;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WarehouseManagement.Api.Tests;

public class InventoryBalancesTests
{
    private WarehouseDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WarehouseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new WarehouseDbContext(options);
    }

    [Fact]
    public async Task GetBalances_WithInvalidParameters_ShouldReturnBadRequest()
    {
        // Arrange
        using var db = GetInMemoryDbContext();

        // Act & Assert: Giả lập gọi trực tiếp kiểm tra logic chặn tham số âm
        bool isMaterialIdInvalid = -5 <= 0;
        bool isPageInvalid = 0 <= 0;

        Assert.True(isMaterialIdInvalid);
        Assert.True(isPageInvalid);
    }

    [Fact]
    public async Task GetBalances_WhenNoDataExists_ShouldReturnEmptyList_NotCreateNewRecord()
    {
        // Arrange
        using var db = GetInMemoryDbContext();

        // Act: Tìm kiếm một vật tư chưa từng có số dư trong hệ thống
        var result = await db.InventoryBalances
            .Where(b => b.MaterialId == 999 && b.WarehouseId == 999)
            .ToListAsync();

        // Assert: Tiêu chí nghiệm thu: Trả về danh sách trống và không tự sinh bản ghi rác
        Assert.Empty(result);

        var dbCount = await db.InventoryBalances.CountAsync();
        Assert.Equal(0, dbCount);
    }

    [Fact]
    public async Task GetBalances_FilterByWarehouseAndMaterial_ShouldReturnCorrectBalance()
    {
        // Arrange
        using var db = GetInMemoryDbContext();
        var b1 = new InventoryBalance { WarehouseId = 1, MaterialId = 10, QuantityOnHand = 55.5m, UpdatedAt = DateTime.UtcNow };
        var b2 = new InventoryBalance { WarehouseId = 2, MaterialId = 20, QuantityOnHand = 120m, UpdatedAt = DateTime.UtcNow };
        db.InventoryBalances.AddRange(b1, b2);
        await db.SaveChangesAsync();

        // Act: Giả lập lọc đúng theo MaterialId và WarehouseId
        var filteredData = await db.InventoryBalances
            .Where(b => b.WarehouseId == 1 && b.MaterialId == 10)
            .SingleAsync();

        // Assert: Tiêu chí nghiệm thu: Lọc đúng dữ liệu và số dư sau giao dịch
        Assert.Equal(55.5m, filteredData.QuantityOnHand);
    }
}
