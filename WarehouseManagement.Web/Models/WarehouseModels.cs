using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Web.Models
{
    public class DashboardSummaryDto
    {
        public int TotalMaterials { get; set; }
        public int TotalWarehouses { get; set; }
        public int TotalActiveInventory { get; set; }
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
    }

    public class RecentActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty; // "Nhập kho" hoặc "Xuất kho"
        public string DocumentCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty; // "Draft", "Posted", "Reversed"
    }

    // --- DTOs cho Vật tư ---
    public class MaterialItemDto
    {
        public int MaterialId { get; set; }

        public string MaterialCode { get; set; } = string.Empty;

        public string MaterialName { get; set; } = string.Empty;

        public string BaseUnit { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    // --- DTOs cho Kho hàng ---
    public class WarehouseItemDto
    {
        public int WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty; 
        public string WarehouseName { get; set; } = string.Empty; 
        public bool IsActive { get; set; }
    }

    // DTO kết quả trả về bao gồm thông tin phân trang từ API
    public class InventoryPagedResultDto
    {
        public List<InventoryItemDto> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class InventoryItemDto
    {
        public int Id { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class TransactionPagedResultDto
    {
        public List<TransactionItemDto> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public class TransactionItemDto
    {
        public int Id { get; set; }
        public string DocumentCode { get; set; } = string.Empty; // Ví dụ: PN-001, PX-002
        public string Type { get; set; } = string.Empty;         // "Inbound" (Nhập) hoặc "Outbound" (Xuất)
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;       // "Draft", "Posted", "Reversed"
        public string Note { get; set; } = string.Empty;
    }

    public class CreateTransactionLineDto
    {
        [Required(ErrorMessage = "Vui lòng chọn vật tư.")]
        public string MaterialCode { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public int Quantity { get; set; } = 1;
    }

    public class TransactionDetailDto
    {
        public int Id { get; set; }
        public string DocumentCode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;         // "Inbound" hoặc "Outbound"
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;       // "Draft", "Posted", "Reversed"
        public string Note { get; set; } = string.Empty;
        public List<TransactionDetailLineDto> Lines { get; set; } = new();
    }

    public class StockSummaryReportDto
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int OpeningQuantity { get; set; } // Tồn đầu kỳ
        public int InboundQuantity { get; set; } // Nhập trong kỳ
        public int OutboundQuantity { get; set; } // Xuất trong kỳ
        public int ClosingQuantity { get; set; } // Tồn cuối kỳ
    }

    // DTO bám sát kết quả trả về của /api/materials và /api/warehouses
    public class PagedMetadataResponse
    {
        public int TotalCount { get; set; }
    }

    // DTO bám sát kết quả trả về của /api/inventory-balances
    public class InventoryBalanceResponseDto
    {
        public List<InventoryBalanceItemDto> Items { get; set; } = new();
    }

    public class InventoryBalanceItemDto
    {
        public int MaterialId { get; set; }
        public int WarehouseId { get; set; }
        public decimal QuantityOnHand { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class BackendTransactionItemDto
    {
        public int StockTransactionId { get; set; }
        public string TransactionNumber { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty; // "StockIn" hoặc "StockOut"
        public string Status { get; set; } = string.Empty;          // "Draft", "Posted", "Cancelled", "Reversed"
        public int WarehouseId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MaterialPagedResponseDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<MaterialItemDto> Items { get; set; } = new(); // Khớp với trường "items" của backend
    }

    public class WarehousePagedResponseDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<WarehouseItemDto> Items { get; set; } = new();
    }

    public class InventoryBalancesPagedResponseDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<InventoryBalanceItemDto> Items { get; set; } = new(); // Khớp trường "items" của backend
    }

    public class TransactionPagedResponseDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<BackendTransactionItemDto> Items { get; set; } = new(); // Khớp thuộc tính "items" backend
    }

    public class StockSummaryReportResponseDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<StockSummaryReportItemDto> Data { get; set; } = new(); // Khớp trường "data" của backend
    }

    public class StockSummaryReportItemDto
    {
        public int WarehouseId { get; set; }
        public int MaterialId { get; set; }
        public decimal OpeningBalance { get; set; } // Khớp trường "openingBalance" backend
        public decimal TotalIn { get; set; }        // Khớp trường "totalIn" backend
        public decimal TotalOut { get; set; }       // Khớp trường "totalOut" backend
        public decimal ClosingBalance { get; set; } // Khớp trường "closingBalance" backend
    }

    public class CreateMaterialDto
    {
        [Required(ErrorMessage = "Mã vật tư không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã vật tư không được vượt quá 50 ký tự.")]
        public string MaterialCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên vật tư không được để trống.")]
        [StringLength(250, ErrorMessage = "Tên vật tư không được vượt quá 250 ký tự.")]
        public string MaterialName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Đơn vị tính không được để trống.")]
        [StringLength(20, ErrorMessage = "Đơn vị tính không được vượt quá 20 ký tự.")]
        public string BaseUnit { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public class UpdateMaterialDto
    {
        [Required(ErrorMessage = "Mã vật tư không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã vật tư không được vượt quá 50 ký tự.")]
        public string MaterialCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên vật tư không được để trống.")]
        [StringLength(250, ErrorMessage = "Tên vật tư không được vượt quá 250 ký tự.")]
        public string MaterialName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Đơn vị tính không được để trống.")]
        [StringLength(20, ErrorMessage = "Đơn vị tính không được vượt quá 20 ký tự.")]
        public string BaseUnit { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    public class CreateWarehouseDto
    {
        [Required(ErrorMessage = "Mã kho hàng không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã kho hàng không được vượt quá 50 ký tự.")]
        public string WarehouseCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên kho hàng không được để trống.")]
        [StringLength(250, ErrorMessage = "Tên kho hàng không được vượt quá 250 ký tự.")]
        public string WarehouseName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }

    public class UpdateWarehouseDto
    {
        [Required(ErrorMessage = "Mã kho hàng không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã kho hàng không được vượt quá 50 ký tự.")]
        public string WarehouseCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên kho hàng không được để trống.")]
        [StringLength(250, ErrorMessage = "Tên kho hàng không được vượt quá 250 ký tự.")]
        public string WarehouseName { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }

    public class CreateTransactionDto
    {
        [Required(ErrorMessage = "Vui lòng chọn kho hàng thực hiện.")]
        public int? WarehouseId { get; set; }

        // Bổ sung trường ID người lập để vượt qua điều kiện kiểm tra dòng 142 của Backend
        public int CreatedByUserId { get; set; } = 1;

        [Required(ErrorMessage = "Vui lòng chọn loại giao dịch.")]
        public string TransactionType { get; set; } = "StockIn";

        public string Reason { get; set; } = string.Empty;

        // Khớp 100% với tên mảng request.Details của máy chủ API
        public List<CreateTransactionDetailDto> Details { get; set; } = new();
    }

    public class CreateTransactionDetailDto
    {
        [Required(ErrorMessage = "Vui lòng chọn vật tư.")]
        public int? MaterialId { get; set; }

        [Required(ErrorMessage = "Số lượng không được để trống.")]
        [Range(0.001, 9999999, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        public decimal Quantity { get; set; } = 1;
    }

    public class TransactionDetailViewDto
    {
        public int StockTransactionId { get; set; }
        public string TransactionNumber { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty; // "StockIn" hoặc "StockOut"
        public string Status { get; set; } = string.Empty;          // "Draft", "Posted", etc.
        public int WarehouseId { get; set; }
        public int CreatedByUserId { get; set; }
        public int? PostedByUserId { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PostedAt { get; set; }

        // Mảng chi tiết vật tư đính kèm trong phiếu kho (Khớp chữ "Details" của API)
        public List<TransactionDetailLineDto> Details { get; set; } = new();
    }

    public class TransactionDetailLineDto
    {
        public int StockTransactionDetailId { get; set; }
        public int MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty; // Khớp trường "materialCode" backend
        public string MaterialName { get; set; } = string.Empty; // Khớp trường "materialName" backend
        public string BaseUnit { get; set; } = string.Empty;     // Khớp trường "baseUnit" backend
        public decimal Quantity { get; set; }
    }
}
