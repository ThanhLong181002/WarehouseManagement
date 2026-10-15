using WarehouseManagement.Web.Components;
using WarehouseManagement.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. ĐĂNG KÝ CÁC DỊCH VỤ VÀO DI CONTAINER (BẮT BUỘC ĐẶT TRƯỚC BUILD)
// =========================================================================

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Đọc và validate cấu hình URL từ appsettings.json
var apiBaseUrl = builder.Configuration["WarehouseApi:BaseUrl"];

if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiUri) ||
    (apiUri.Scheme != Uri.UriSchemeHttp && apiUri.Scheme != Uri.UriSchemeHttps))
{
    throw new InvalidOperationException(
        "WarehouseApi:BaseUrl phải là URL HTTP/HTTPS hợp lệ.");
}

// ✅ ĐÃ SỬA CHÍNH XÁC: Di chuyển cấu hình HttpClient lên trước builder.Build()
builder.Services.AddHttpClient("WarehouseApi", client =>
{
    client.BaseAddress = apiUri;
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ✅ ĐÃ SỬA CHÍNH XÁC: Di chuyển đăng ký các Scoped Service lên trước builder.Build()
builder.Services.AddScoped<TokenSession>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<WarehouseApiClient>();

// Đăng ký HttpClient với địa chỉ cấu hình động (Đã sửa: Truyền trực tiếp đối tượng apiUri)
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = apiUri });

// =========================================================================
// 2. KHỞI TẠO MÁY CHỦ ỨNG DỤNG (CHỐNG LỖI READ-ONLY)
// =========================================================================
var app = builder.Build();

// =========================================================================
// 3. CẤU HÌNH HTTP REQUEST PIPELINE (MIDDLEWARE ĐẶT SAU BUILD)
// =========================================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
