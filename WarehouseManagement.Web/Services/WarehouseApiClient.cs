using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WarehouseManagement.Web.Services;

public sealed class ApiUnauthorizedException()
    : Exception("Phiên đăng nhập không còn hợp lệ.");

public sealed class ApiForbiddenException()
    : Exception("Bạn không có quyền thực hiện thao tác này.");

public sealed class WarehouseApiClient(
    IHttpClientFactory factory,
    TokenSession session)
{
    private readonly HttpClient _client =
        factory.CreateClient("WarehouseApi");

    public async Task<T?> GetAsync<T>(
        string path,
        CancellationToken cancellationToken = default)
    {
        // ✅ ĐÃ SỬA: Tạo Request độc lập và đính kèm Token an toàn
        using var request = CreateRequest(HttpMethod.Get, path);
        using var response = await _client.SendAsync(
            request, cancellationToken);

        EnsureAuthorized(response);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(
            cancellationToken: cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        // ✅ ĐÃ SỬA: Tạo Request độc lập và đính kèm Token an toàn
        using var request = CreateRequest(HttpMethod.Post, path);
        request.Content = JsonContent.Create(body);

        using var response = await _client.SendAsync(
            request, cancellationToken);

        EnsureAuthorized(response);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TResponse>(
            cancellationToken: cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string path,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        // ✅ ĐÃ SỬA: Tạo Request độc lập và đính kèm Token an toàn
        using var request = CreateRequest(HttpMethod.Put, path);
        request.Content = JsonContent.Create(body);

        using var response = await _client.SendAsync(
            request, cancellationToken);

        EnsureAuthorized(response);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TResponse>(
            cancellationToken: cancellationToken);
    }

    private HttpRequestMessage CreateRequest(
    HttpMethod method,
    string path)
    {
        var request = new HttpRequestMessage(method, path);

        // ✅ ĐÃ SỬA CHÍNH XÁC: Chỉ sử dụng thuộc tính AccessToken duy nhất từ TokenSession của bạn
        if (session.IsAuthenticated && !string.IsNullOrWhiteSpace(session.AccessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }

        return request;
    }


    private static void EnsureAuthorized(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new ApiUnauthorizedException();

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new ApiForbiddenException();
    }
}
