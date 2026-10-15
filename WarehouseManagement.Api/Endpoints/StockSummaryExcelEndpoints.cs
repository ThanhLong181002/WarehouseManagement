using OfficeOpenXml;
using OfficeOpenXml.Style;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Api.Data;

namespace WarehouseManagement.Api.Endpoints;

public static class StockSummaryExcelEndpoints
{
    public static IEndpointRouteBuilder MapStockSummaryExcelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/stock-summary/export", async (
            int? materialId,
            int? warehouseId,
            DateTime fromDate,
            DateTime toDate,
            WarehouseDbContext db,
            CancellationToken cancellationToken) =>
        {
            try
            {
                // --- 1. Ràng buộc điều kiện đầu vào (Nhất quán với Step 5.3) ---
                if (fromDate > toDate)
                {
                    return Results.BadRequest(new { message = "fromDate cannot be later than toDate." });
                }

                var startOfFromDate = fromDate.Date;
                var endOfToDate = toDate.Date.AddDays(1).AddTicks(-1);

                // --- 2. Tái sử dụng logic lấy dữ liệu và tính toán từ Step 5.3 ---
                var balanceQuery = db.InventoryBalances.AsNoTracking();
                if (warehouseId.HasValue) balanceQuery = balanceQuery.Where(b => b.WarehouseId == warehouseId.Value);
                if (materialId.HasValue) balanceQuery = balanceQuery.Where(b => b.MaterialId == materialId.Value);
                var currentBalances = await balanceQuery.ToListAsync(cancellationToken);

                var txQuery = db.StockTransactions
                    .Include(t => t.StockTransactionDetails)
                    .Where(t => t.Status == "Posted" || t.Status == "Reversed")
                    .AsNoTracking();
                if (warehouseId.HasValue) txQuery = txQuery.Where(t => t.WarehouseId == warehouseId.Value);
                if (materialId.HasValue) txQuery = txQuery.Where(t => t.StockTransactionDetails.Any(d => d.MaterialId == materialId.Value));
                var allTransactions = await txQuery.ToListAsync(cancellationToken);

                var targetKeys = currentBalances.Select(b => new { b.WarehouseId, b.MaterialId })
                    .Union(allTransactions.SelectMany(t => t.StockTransactionDetails.Select(d => new { t.WarehouseId, d.MaterialId })))
                    .Distinct()
                    .ToList();

                // THAY BẰNG DÒNG NÀY:
                OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("WarehouseManagement");

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Nhap_Xuat_Ton");

                // Cấu hình định dạng chung cho bảng dữ liệu
                worksheet.Cells.Style.Font.Name = "Arial";
                worksheet.Cells.Style.Font.Size = 11;

                // Thiết lập Tiêu đề báo cáo (Dòng 1 & Dòng 2)
                worksheet.Cells["A1:F1"].Merge = true;
                worksheet.Cells["A1"].Value = "BÁO CÁO TỔNG HỢP NHẬP - XUẤT - TỒN KHO";
                worksheet.Cells["A1"].Style.Font.Size = 16;
                worksheet.Cells["A1"].Style.Font.Bold = true;
                worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                worksheet.Cells["A2:F2"].Merge = true;
                worksheet.Cells["A2"].Value = $"Từ ngày: {startOfFromDate:dd/MM/yyyy}  -  Đến ngày: {toDate:dd/MM/yyyy}";
                worksheet.Cells["A2"].Style.Font.Italic = true;
                worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Định dạng Cột Tiêu đề (Dòng 4)
                string[] headers = { "Mã Kho", "Mã Vật Tư", "Tồn Đầu Kỳ", "Tổng Nhập", "Tổng Xuất", "Tồn Cuối Kỳ" };
                for (int i = 0; i < headers.Length; i++)
                {
                    var cell = worksheet.Cells[4, i + 1];
                    cell.Value = headers[i];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(31, 78, 120)); // Navy Blue sang trọng
                    cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
                    cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // --- 4. Điền dữ liệu vào các dòng Excel ---
                int currentRow = 5;
                bool hasData = false;

                foreach (var key in targetKeys)
                {
                    var currentQty = currentBalances.FirstOrDefault(b => b.WarehouseId == key.WarehouseId && b.MaterialId == key.MaterialId)?.QuantityOnHand ?? 0m;
                    var details = allTransactions.Where(t => t.WarehouseId == key.WarehouseId).SelectMany(t => t.StockTransactionDetails.Where(d => d.MaterialId == key.MaterialId).Select(d => new { t.TransactionType, t.CreatedAt, d.Quantity })).ToList();

                    decimal totalInAfterPeriod = details.Where(d => d.CreatedAt > endOfToDate && d.TransactionType == "StockIn").Sum(d => d.Quantity);
                    decimal totalOutAfterPeriod = details.Where(d => d.CreatedAt > endOfToDate && d.TransactionType == "StockOut").Sum(d => d.Quantity);

                    decimal closingBalance = currentQty - totalInAfterPeriod + totalOutAfterPeriod;
                    decimal totalInDuringPeriod = details.Where(d => d.CreatedAt >= startOfFromDate && d.CreatedAt <= endOfToDate && d.TransactionType == "StockIn").Sum(d => d.Quantity);
                    decimal totalOutDuringPeriod = details.Where(d => d.CreatedAt >= startOfFromDate && d.CreatedAt <= endOfToDate && d.TransactionType == "StockOut").Sum(d => d.Quantity);
                    decimal openingBalance = closingBalance - totalInDuringPeriod + totalOutDuringPeriod;

                    // Quy tắc: Chỉ xuất dòng nếu có số dư hoặc có phát sinh biến động
                    if (openingBalance != 0 || totalInDuringPeriod != 0 || totalOutDuringPeriod != 0 || closingBalance != 0)
                    {
                        hasData = true;
                        worksheet.Cells[currentRow, 1].Value = key.WarehouseId;
                        worksheet.Cells[currentRow, 2].Value = key.MaterialId;
                        worksheet.Cells[currentRow, 3].Value = openingBalance;
                        worksheet.Cells[currentRow, 4].Value = totalInDuringPeriod;
                        worksheet.Cells[currentRow, 5].Value = totalOutDuringPeriod;
                        worksheet.Cells[currentRow, 6].Value = closingBalance;

                        // Cấu hình căn lề và định dạng số cho các cột Số lượng (3 chữ số thập phân phù hợp với hệ thống)
                        for (int col = 3; col <= 6; col++)
                        {
                            worksheet.Cells[currentRow, col].Style.Numberformat.Format = "#,##0.000";
                            worksheet.Cells[currentRow, col].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        }

                        // Thêm đường viền ô mỏng
                        for (int col = 1; col <= 6; col++)
                        {
                            worksheet.Cells[currentRow, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                        }

                        currentRow++;
                    }
                }

                // Xử lý trường hợp báo cáo rỗng (Không có biến động & không có tồn kho)
                if (!hasData)
                {
                    worksheet.Cells["A5:F5"].Merge = true;
                    worksheet.Cells["A5"].Value = "Không có dữ liệu phù hợp với bộ lọc hiện tại.";
                    worksheet.Cells["A5"].Style.Font.Italic = true;
                    worksheet.Cells["A5"].Style.Font.Color.SetColor(System.Drawing.Color.Red);
                    worksheet.Cells["A5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    for (int col = 1; col <= 6; col++)
                    {
                        worksheet.Cells["A5:F5"].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }
                }

                // Tự động căn chỉnh độ rộng của các cột dữ liệu cho vừa vặn, dễ đọc
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // --- 5. Trả về File dạng Stream với MIME type chính xác ---
                var stream = new MemoryStream();
                await package.SaveAsAsync(stream, cancellationToken);
                stream.Position = 0;

                string fileName = $"StockSummaryReport_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
                return Results.File(
                    stream,
                    contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileDownloadName: fileName);
            }
            catch (Exception)
            {
                return Results.Problem(statusCode: 500, detail: "Đã có lỗi hệ thống xảy ra khi xuất báo cáo.");
            }
        });

        return app;
    }
}
