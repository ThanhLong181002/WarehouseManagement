using System.Net.Http.Json;
using System.Text.Json;

namespace WarehouseManagement.Web.Services;

public sealed class LoginService
{
    private readonly HttpClient _httpClient;
    private readonly TokenSession _tokenSession;

    public LoginService(HttpClient httpClient, TokenSession tokenSession)
    {
        _httpClient = httpClient;
        _tokenSession = tokenSession;
    }

    // 🌟 SỬA CHÍNH XÁC: Thay đổi chữ ký hàm nhận vào 2 tham số chuỗi trần để khớp với trang Login.razor
    public async Task<LoginResultDto> LoginAsync(string username, string password)
    {
        try
        {
            // Đóng gói dữ liệu đầu vào gửi lên máy chủ API
            var loginPayload = new { Username = username, Password = password };
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginPayload);

            if (response.IsSuccessStatusCode)
            {
                // Ép bỏ qua phân biệt hoa thường để đọc trúng 'token', 'username', 'role' từ API Backend
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>(jsonOptions);

                if (result != null && !string.IsNullOrWhiteSpace(result.Token))
                {
                    // Ghi dữ liệu bảo mật vào LocalStorage thông qua TokenSession
                    _tokenSession.SignIn(result.Token, result.Username, result.Role);
                    return new LoginResultDto { Success = true };
                }
            }

            // Đọc thông báo lỗi nghiệp vụ chi tiết phát ra từ Backend nếu đăng nhập thất bại
            var errorResult = await response.Content.ReadFromJsonAsync<JsonElement>();
            string errorMessage = errorResult.TryGetProperty("message", out var msg)
                ? msg.GetString() ?? "Tài khoản hoặc mật khẩu không chính xác."
                : "Tài khoản hoặc mật khẩu không chính xác.";

            return new LoginResultDto { Success = false, Message = errorMessage };
        }
        catch (Exception)
        {
            return new LoginResultDto { Success = false, Message = "Không thể kết nối đến máy chủ xác thực." };
        }
    }
}

// --- CÁC ĐỐI TƯỢNG ĐI KÈM TRONG LỚP DỊCH VỤ ---
public sealed class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class LoginResultDto
{
    public bool Success { get; set; }

    // 🌟 KHÔI PHỤC: Sử dụng trường 'Message' đồng bộ 100% với dòng code result.Message ở trang Login.razor
    public string? Message { get; set; }
}
