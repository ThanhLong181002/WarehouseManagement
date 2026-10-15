# Database Setup

## Requirements

- Microsoft SQL Server.
- SSMS hoặc công cụ SQL tương đương.

## Database

Database dùng cho dự án: `WarehouseManagement`.

## Initialization

Các SQL scripts sẽ được lưu tại thư mục này sau khi được xuất và xác minh từ database hoặc EF Core migrations hiện có.

Thứ tự thực thi chính thức phải được ghi rõ sau khi hoàn thiện các scripts.

## Validation

Sau khi khởi tạo database:

1. Kiểm tra các bảng, khóa chính, khóa ngoại, index và constraint.
2. Kiểm tra dữ liệu seed phục vụ demo.
3. Cấu hình connection string cho Backend API.
4. Chạy ứng dụng và kiểm tra các nghiệp vụ kho.

## Security

Chỉ sử dụng dữ liệu mẫu đã làm sạch. Không đưa mật khẩu thật, dữ liệu cá nhân hoặc dữ liệu sản xuất lên repository công khai.
