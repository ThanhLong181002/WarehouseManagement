using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Api.Data;

namespace WarehouseManagement.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inventory-balances", async (
            int? materialId,
            int? warehouseId,
            int? page,
            int? pageSize,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            // Quy tắc 4: Kiểm tra tham số đầu vào không hợp lệ, không trả lỗi hệ thống (500)
            if (materialId.HasValue && materialId.Value <= 0)
            {
                return Results.BadRequest(new { message = "materialId must be greater than 0." });
            }

            if (warehouseId.HasValue && warehouseId.Value <= 0)
            {
                return Results.BadRequest(new { message = "warehouseId must be greater than 0." });
            }

            // Quy tắc 5: Phân trang ổn định với giá trị mặc định an toàn
            int activePage = page ?? 1;
            int activePageSize = pageSize ?? 10;

            if (activePage <= 0 || activePageSize <= 0)
            {
                return Results.BadRequest(new { message = "page and pageSize must be greater than 0." });
            }

            if (activePageSize > 100) // Giới hạn trên để tránh làm quá tải bộ nhớ
            {
                return Results.BadRequest(new { message = "pageSize cannot exceed 100 items." });
            }

            // Quy tắc 1 & 2: Chỉ đọc trực tiếp từ bảng InventoryBalances
            var query = db.InventoryBalances.AsNoTracking();

            // Hỗ trợ tra cứu theo vật tư, theo kho hoặc kết hợp cả hai
            if (materialId.HasValue)
            {
                query = query.Where(b => b.MaterialId == materialId.Value);
            }

            if (warehouseId.HasValue)
            {
                query = query.Where(b => b.WarehouseId == warehouseId.Value);
            }

            // Lấy tổng số dòng để phục vụ siêu dữ liệu phân trang (Metadata)
            int totalItems = await query.CountAsync(cancellationToken);

            // Quy tắc 5: Sắp xếp xác định (bắt buộc trước khi Skip/Take trong EF Core)
            var pagedBalances = await query
                .OrderBy(b => b.WarehouseId)
                .ThenBy(b => b.MaterialId)
                .Skip((activePage - 1) * activePageSize)
                .Take(activePageSize)
                .Select(b => new
                {
                    b.MaterialId,
                    b.WarehouseId,
                    b.QuantityOnHand,
                    b.UpdatedAt // Quy tắc 3: Trả về đầy đủ thông tin định danh và thời điểm cập nhật
                })
                .ToListAsync(cancellationToken);

            // Quy tắc 6: Trả về kết quả phân trang ổn định, không tự động tạo bản ghi mới khi không tìm thấy số dư
            return Results.Ok(new
            {
                items = pagedBalances,
                page = activePage,
                pageSize = activePageSize,
                totalItems = totalItems,
                totalPages = (int)Math.Ceiling((double)totalItems / activePageSize)
            });
        });

        return app;
    }
}
