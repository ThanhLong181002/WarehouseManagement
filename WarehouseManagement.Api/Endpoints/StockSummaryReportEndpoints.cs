using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Api.Data;

namespace WarehouseManagement.Api.Endpoints;

public static class StockSummaryReportEndpoints
{
    public static IEndpointRouteBuilder MapStockSummaryReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/stock-summary", async (
            int? materialId,
            int? warehouseId,
            DateTime fromDate,
            DateTime toDate,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            // --- 1. Ràng buộc điều kiện đầu vào ---
            if (fromDate > toDate)
            {
                return Results.BadRequest(new { message = "fromDate cannot be later than toDate." });
            }

            var startOfFromDate = fromDate.Date;
            var endOfToDate = toDate.Date.AddDays(1).AddTicks(-1);
            var now = DateTime.UtcNow;

            // --- 2. Lấy dữ liệu Số dư Hiện tại từ InventoryBalances ---
            var balanceQuery = db.InventoryBalances.AsNoTracking();
            if (warehouseId.HasValue) balanceQuery = balanceQuery.Where(b => b.WarehouseId == warehouseId.Value);
            if (materialId.HasValue) balanceQuery = balanceQuery.Where(b => b.MaterialId == materialId.Value);

            var currentBalances = await balanceQuery.ToListAsync(cancellationToken);

            // --- 3. Lấy tất cả giao dịch biến động (Posted & Reversed) để tính toán ---
            var txQuery = db.StockTransactions
                .Include(t => t.StockTransactionDetails)
                .Where(t => t.Status == "Posted" || t.Status == "Reversed")
                .AsNoTracking();

            if (warehouseId.HasValue) txQuery = txQuery.Where(t => t.WarehouseId == warehouseId.Value);
            if (materialId.HasValue) txQuery = txQuery.Where(t => t.StockTransactionDetails.Any(d => d.MaterialId == materialId.Value));

            var allTransactions = await txQuery.ToListAsync(cancellationToken);

            // --- 4. Tổng hợp dữ liệu theo cặp (WarehouseId, MaterialId) ---
            // Thu thập tất cả các cặp Kho - Vật tư xuất hiện trong số dư hoặc trong lịch sử giao dịch
            var targetKeys = currentBalances.Select(b => new { b.WarehouseId, b.MaterialId })
                .Union(allTransactions.SelectMany(t => t.StockTransactionDetails.Select(d => new { t.WarehouseId, d.MaterialId })))
                .Distinct()
                .ToList();

            var reportRows = new List<object>();

            foreach (var key in targetKeys)
            {
                // Lấy số dư hiện tại (nếu chưa có thì mặc định bằng 0)
                var currentQty = currentBalances
                    .FirstOrDefault(b => b.WarehouseId == key.WarehouseId && b.MaterialId == key.MaterialId)?
                    .QuantityOnHand ?? 0m;

                // Chi tiết giao dịch của cặp Kho - Vật tư này
                var details = allTransactions
                    .Where(t => t.WarehouseId == key.WarehouseId)
                    .SelectMany(t => t.StockTransactionDetails
                        .Where(d => d.MaterialId == key.MaterialId)
                        .Select(d => new { t.TransactionType, t.CreatedAt, d.Quantity }))
                    .ToList();

                // Tính biến động SAU kỳ báo cáo (Từ endOfToDate đến hiện tại) để tính ngược dòng ra Tồn cuối kỳ
                decimal totalInAfterPeriod = details.Where(d => d.CreatedAt > endOfToDate && d.TransactionType == "StockIn").Sum(d => d.Quantity);
                decimal totalOutAfterPeriod = details.Where(d => d.CreatedAt > endOfToDate && d.TransactionType == "StockOut").Sum(d => d.Quantity);

                // Tồn cuối kỳ báo cáo = Tồn hiện tại - Nhập sau kỳ + Xuất sau kỳ
                decimal closingBalance = currentQty - totalInAfterPeriod + totalOutAfterPeriod;

                // Tính biến động TRONG kỳ báo cáo (Từ startOfFromDate đến endOfToDate)
                decimal totalInDuringPeriod = details.Where(d => d.CreatedAt >= startOfFromDate && d.CreatedAt <= endOfToDate && d.TransactionType == "StockIn").Sum(d => d.Quantity);
                decimal totalOutDuringPeriod = details.Where(d => d.CreatedAt >= startOfFromDate && d.CreatedAt <= endOfToDate && d.TransactionType == "StockOut").Sum(d => d.Quantity);

                // Tồn đầu kỳ báo cáo = Tồn cuối kỳ báo cáo - Nhập trong kỳ + Xuất trong kỳ
                decimal openingBalance = closingBalance - totalInDuringPeriod + totalOutDuringPeriod;


                // Chỉ hiển thị lên báo cáo nếu có số dư hoặc có biến động trong kỳ
                if (openingBalance != 0 || totalInDuringPeriod != 0 || totalOutDuringPeriod != 0 || closingBalance != 0)
                {
                    reportRows.Add(new
                    {
                        key.WarehouseId,
                        key.MaterialId,
                        openingBalance,
                        totalIn = totalInDuringPeriod,
                        totalOut = totalOutDuringPeriod,
                        closingBalance
                    });
                }
            }

            return Results.Ok(new
            {
                fromDate = startOfFromDate,
                toDate = endOfToDate,
                reportGeneratedAt = now,
                data = reportRows
            });
        });

        return app;
    }
}
