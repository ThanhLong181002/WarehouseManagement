using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Api.Data;

namespace WarehouseManagement.Api.Endpoints;

public static class StockTransactionHistoryEndpoints
{
    public static IEndpointRouteBuilder MapStockTransactionHistoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/stock-transactions/history", async (
            int? materialId,
            int? warehouseId,
            string? transactionType,
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            int? page,
            int? pageSize,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            // --- 1. Kiểm tra tham số đầu vào không hợp lệ ---
            if (materialId.HasValue && materialId.Value <= 0)
            {
                return Results.BadRequest(new { message = "materialId must be greater than 0." });
            }

            if (warehouseId.HasValue && warehouseId.Value <= 0)
            {
                return Results.BadRequest(new { message = "warehouseId must be greater than 0." });
            }

            if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
            {
                return Results.BadRequest(new { message = "fromDate cannot be later than toDate." });
            }

            int activePage = page ?? 1;
            int activePageSize = pageSize ?? 10;

            if (activePage <= 0 || activePageSize <= 0)
            {
                return Results.BadRequest(new { message = "page and pageSize must be greater than 0." });
            }

            if (activePageSize > 100)
            {
                return Results.BadRequest(new { message = "pageSize cannot exceed 100 items." });
            }

            // --- 2. Xây dựng Query đọc dữ liệu (Chỉ đọc - AsNoTracking) ---
            var query = db.StockTransactions
                .Include(t => t.StockTransactionDetails)
                .AsNoTracking();

            // Lọc theo Kho
            if (warehouseId.HasValue)
            {
                query = query.Where(t => t.WarehouseId == warehouseId.Value);
            }

            // Lọc theo Loại giao dịch (StockIn, StockOut)
            if (!string.IsNullOrWhiteSpace(transactionType))
            {
                query = query.Where(t => t.TransactionType == transactionType);
            }

            // Lọc theo Trạng thái (Draft, Posted, Cancelled, Reversed)
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            // Lọc theo ranh giới khoảng ngày chính xác (Bao gồm cả ngày cuối cùng)
            if (fromDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                // Đảm bảo lấy hết dữ liệu của ngày toDate bằng cách chặn đến hết ngày (23:59:59)
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.CreatedAt <= endOfDay);
            }

            // Lọc sâu theo Vật tư dựa trên bảng chi tiết giao dịch
            if (materialId.HasValue)
            {
                query = query.Where(t => t.StockTransactionDetails.Any(d => d.MaterialId == materialId.Value));
            }

            // --- 3. Tính tổng số dòng và Phân trang ổn định ---
            int totalItems = await query.CountAsync(cancellationToken);

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt) // Sắp xếp giảm dần theo thời gian tạo để đẩy phiếu mới lên trước
                .ThenBy(t => t.StockTransactionId)   // Sắp xếp phụ để đảm bảo phân trang ổn định không trùng lặp
                .Skip((activePage - 1) * activePageSize)
                .Take(activePageSize)
                .Select(t => new
                {
                    t.StockTransactionId,
                    t.TransactionNumber,
                    t.TransactionType,
                    t.Status,
                    t.WarehouseId,
                    t.ReversesTransactionId, // Hiển thị liên kết tới giao dịch gốc
                    t.Reason,
                    t.CreatedAt,
                    t.PostedAt,
                    t.UpdatedAt,
                    Details = t.StockTransactionDetails.Select(d => new
                    {
                        d.MaterialId,
                        d.Quantity
                    })
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new
            {
                items = transactions,
                page = activePage,
                pageSize = activePageSize,
                totalItems = totalItems,
                totalPages = (int)Math.Ceiling((double)totalItems / activePageSize)
            });
        });

        return app;
    }
}
