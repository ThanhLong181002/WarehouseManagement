# WarehouseManagement

Hệ thống quản lý kho vật tư được xây dựng bằng ASP.NET Core Web API, Blazor Web App và Microsoft SQL Server.

## 1. Các project

- `WarehouseManagement.Api/`: Backend REST API.
- `WarehouseManagement.Web/`: Frontend Blazor Web App.
- `WarehouseManagement.Api.Tests/`: Automated tests.
- `database/`: SQL scripts và hướng dẫn khởi tạo database.
- `docs/`: Tài liệu kiến trúc và ảnh giao diện.

## 2. Công nghệ

- .NET 10
- ASP.NET Core Web API
- Blazor Web App
- Microsoft SQL Server
- JWT Bearer Authentication
- .NET automated tests

## 3. Chức năng

- Quản lý vật tư và kho.
- Quản lý giao dịch nhập, xuất, điều chỉnh và đảo ngược kho theo các quy tắc đã triển khai.
- Tra cứu số lượng tồn kho.
- Tra cứu lịch sử giao dịch.
- Báo cáo tổng hợp tồn kho và xuất Excel.
- Đăng nhập và xác thực JWT.

## 4. Yêu cầu môi trường

- .NET 10 SDK.
- Microsoft SQL Server.
- SQL Server Management Studio (SSMS) hoặc công cụ SQL tương đương.
- Git.

## 5. Khởi tạo database

Đọc `database/README.md` để biết cách tạo database từ đầu.

Không sử dụng connection string hoặc thông tin đăng nhập cá nhân của người phát triển.

## 6. Build và kiểm thử

```bash
dotnet restore WarehouseManagement.slnx
dotnet build WarehouseManagement.slnx --configuration Release
dotnet test WarehouseManagement.Api.Tests/WarehouseManagement.Api.Tests.csproj --configuration Release
```

## 7. Chạy ứng dụng

Chạy Backend API và Frontend Web trong hai terminal riêng. Sử dụng URL thực tế được hiển thị khi ứng dụng khởi động.

Cấu hình connection string, JWT signing key và API base URL theo hướng dẫn dành cho môi trường local.

## 8. Tài liệu

- Kiến trúc: `docs/architecture.md`
- SQL Server: `database/README.md`
- Ảnh giao diện: `docs/screenshots/`

## 9. Security

Không commit mật khẩu, JWT signing key, token, dữ liệu cá nhân hoặc database backup chứa dữ liệu nhạy cảm.

## 10. License

Chưa xác định. Bổ sung license phù hợp trước khi công bố quyền sử dụng mã nguồn.
