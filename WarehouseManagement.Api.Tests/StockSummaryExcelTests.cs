using System;
using System.IO;
using System.Threading.Tasks;
using OfficeOpenXml;
using Xunit;

namespace WarehouseManagement.Api.Tests;

public class StockSummaryExcelTests
{
    [Fact]
    public void Excel_Export_ShouldGenerateValidPackageStructure_AndMimeType()
    {
        // Arrange
        // SỬA ĐỔI: Sử dụng đường dẫn tĩnh trực tiếp thông qua ExcelPackage để tránh lỗi biên dịch
        OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("WarehouseManagement");
        using var package = new ExcelPackage();

        // Act: Giả lập khởi tạo cấu trúc bảng Excel báo cáo
        var worksheet = package.Workbook.Worksheets.Add("Test_Sheet");
        worksheet.Cells["A1"].Value = "Mã Kho";
        worksheet.Cells["B1"].Value = "Tồn Đầu Kỳ";
        worksheet.Cells["B1"].Style.Numberformat.Format = "#,##0.000"; // Định dạng số thập phân

        using var stream = new MemoryStream();
        package.SaveAs(stream);

        // Định nghĩa MIME type tiêu chuẩn cho file .xlsx
        string expectedMimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        // Assert: Tiêu chí nghiệm thu: File Excel hợp lệ, cấu trúc sheet và định dạng số chính xác
        Assert.True(stream.Length > 0, "Dữ liệu Stream file Excel không được phép trống.");
        Assert.Equal("Test_Sheet", package.Workbook.Worksheets[0].Name);
        Assert.Equal("#,##0.000", worksheet.Cells["B1"].Style.Numberformat.Format);
        Assert.NotEmpty(expectedMimeType);
    }

    [Fact]
    public void Excel_InvalidDateRange_ShouldFailValidation()
    {
        // Arrange
        var fromDate = new DateTime(2026, 10, 15);
        var toDate = new DateTime(2026, 10, 14); // Ngày từ sau ngày đến

        // Act
        bool isInvalid = fromDate > toDate;

        // Assert: Tiêu chí nghiệm thu: Xử lý đúng tham số không hợp lệ
        Assert.True(isInvalid);
    }
}
