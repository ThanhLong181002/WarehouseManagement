using System;
using System.Text.Json;
using Microsoft.JSInterop;

namespace WarehouseManagement.Web.Services;

public sealed class TokenSession
{
    private readonly IJSRuntime _js;
    private const string StorageKey = "WM_UserSession";

    public string? AccessToken { get; private set; }
    public string? Username { get; private set; }
    public string? Role { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);
    public event Action? OnSessionChanged;

    // Inject IJSRuntime thông qua Primary Constructor hoặc Constructor truyền thống
    public TokenSession(IJSRuntime js)
    {
        _js = js;
    }

    public void SignIn(string token, string username, string role)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token không được để trống.", nameof(token));

        AccessToken = token;
        Username = username;
        Role = role;

        // 🌟 Đồng bộ lưu trữ xuống LocalStorage ngầm (Fire-and-forget an toàn trong Blazor Server)
        var data = new LocalSessionData(token, username, role);
        _ = _js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(data));

        NotifyStateChanged();
    }

    public void SignOut()
    {
        AccessToken = null;
        Username = null;
        Role = null;

        // 🧹 Xóa sạch dấu vết trong trình duyệt
        _ = _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);

        NotifyStateChanged();
    }

    public void ClearSession() => SignOut();

    // 🚀 Hàm cực kỳ quan trọng: Khôi phục phiên làm việc khi người dùng F5 hoặc ForceLoad trang
    public async Task InitializeAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var data = JsonSerializer.Deserialize<LocalSessionData>(json);
                if (data != null && !string.IsNullOrWhiteSpace(data.Token))
                {
                    // Ép ghi dữ liệu vào thuộc tính RAM cục bộ ngay lập tức
                    AccessToken = data.Token;
                    Username = data.Username;
                    Role = data.Role;

                    NotifyStateChanged();
                    return; // Kết thúc thành công
                }
            }
        }
        catch
        {
            // Bọc lỗi an toàn đề phòng Prerender gọi nhầm
        }

        // Nếu không tìm thấy hoặc lỗi, đưa về null
        AccessToken = null;
    }


    private void NotifyStateChanged() => OnSessionChanged?.Invoke();

    private sealed record LocalSessionData(string Token, string Username, string Role);
}
