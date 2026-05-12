using System.Net.Http.Json;

namespace SocialApp.Web.Services;

using System.Net.Http.Headers;

public sealed class SocialAppApiClient(HttpClient http)
{
    public async Task<AuthResult?> CreateAccountAsync(CreateAccountRequest request) =>
        await ReadAsync<AuthResult>(await http.PostAsJsonAsync("/api/accounts", request));

    public async Task<SimpleResult?> RegisterAccountAsync(RegisterAccountRequest request) =>
        await ReadAsync<SimpleResult>(await http.PostAsJsonAsync("/api/registrations", request));

    public async Task<SimpleResult?> VerifyRegistrationAsync(VerifyRegistrationRequest request) =>
        await ReadAsync<SimpleResult>(await http.PostAsJsonAsync("/api/registrations/verify", request));

    public async Task<AuthResult?> LoginAsync(LoginRequest request) =>
        await ReadAsync<AuthResult>(await http.PostAsJsonAsync("/api/sessions", request));

    public async Task<DeviceLoginResult?> LoginWithDeviceAsync(LoginWithDeviceRequest request) =>
        await ReadAsync<DeviceLoginResult>(await http.PostAsJsonAsync("/api/sessions/device", request));

    public async Task<AuthResult?> VerifyDeviceOtpAsync(VerifyDeviceOtpRequest request) =>
        await ReadAsync<AuthResult>(await http.PostAsJsonAsync("/api/sessions/device/verify", request));

    public async Task<SimpleResult?> RequestPasswordResetAsync(RequestPasswordResetRequest request) =>
        await ReadAsync<SimpleResult>(await http.PostAsJsonAsync("/api/password-reset-requests", request));

    public async Task<SimpleResult?> ResetPasswordAsync(ResetPasswordRequest request) =>
        await ReadAsync<SimpleResult>(await http.PostAsJsonAsync("/api/password-resets", request));

    public async Task<CreatePostResult?> CreatePostAsync(string sessionToken, CreatePostRequest request)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/posts")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        return await ReadAsync<CreatePostResult>(await http.SendAsync(message));
    }

    public async Task<SimpleResult?> DeletePostAsync(string sessionToken, Guid postId)
    {
        using var message = new HttpRequestMessage(HttpMethod.Delete, $"/api/posts/{postId}");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        return await ReadAsync<SimpleResult>(await http.SendAsync(message));
    }

    public async Task<CreatePostResult?> RepostAsync(string sessionToken, Guid postId, RepostPostRequest request)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/posts/{postId}/reposts")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        return await ReadAsync<CreatePostResult>(await http.SendAsync(message));
    }

    public async Task<SimpleResult?> DeleteMyRepostAsync(string sessionToken, Guid postId)
    {
        using var message = new HttpRequestMessage(HttpMethod.Delete, $"/api/posts/{postId}/reposts/mine");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        return await ReadAsync<SimpleResult>(await http.SendAsync(message));
    }

    public async Task<SimpleResult?> LikePostAsync(string sessionToken, Guid postId)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/posts/{postId}/likes");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        return await ReadAsync<SimpleResult>(await http.SendAsync(message));
    }

    public async Task<SimpleResult?> DeleteLikeAsync(string sessionToken, Guid postId)
    {
        using var message = new HttpRequestMessage(HttpMethod.Delete, $"/api/posts/{postId}/likes");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        return await ReadAsync<SimpleResult>(await http.SendAsync(message));
    }

    public async Task<RecentPostsResult?> GetRecentPostsAsync(string readerHandle, int limit = 20) =>
        await http.GetFromJsonAsync<RecentPostsResult>($"/api/posts/recent?readerHandle={Uri.EscapeDataString(readerHandle)}&limit={limit}");

    public async Task<RecentPostsResult?> SearchPostsAsync(string readerHandle, string query) =>
        await http.GetFromJsonAsync<RecentPostsResult>($"/api/posts/search?readerHandle={Uri.EscapeDataString(readerHandle)}&query={Uri.EscapeDataString(query)}");

    public async Task<UserProfileResult?> GetUserAsync(string handle) =>
        await ReadAsync<UserProfileResult>(await http.GetAsync($"/api/users/{Uri.EscapeDataString(handle)}"));

    private static async Task<T?> ReadAsync<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode &&
            response.StatusCode != System.Net.HttpStatusCode.BadRequest &&
            response.StatusCode != System.Net.HttpStatusCode.Conflict)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }
}

public sealed record CreateAccountRequest(string DisplayName, string Handle, string Email, string Password);
public sealed record RegisterAccountRequest(string DisplayName, string Handle, string Email, string Password);
public sealed record VerifyRegistrationRequest(string Email, string Code);
public sealed record LoginRequest(string Email, string Password);
public sealed record LoginWithDeviceRequest(string Email, string Password, string DeviceId);
public sealed record VerifyDeviceOtpRequest(string Handle, string DeviceId, string Code, bool RememberDevice);
public sealed record RequestPasswordResetRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record CreatePostRequest(string Content);
public sealed record RepostPostRequest(string Content);
public sealed record AuthResult(bool Succeeded, string Message, string? Handle, string? SessionToken);
public sealed record DeviceLoginResult(bool Succeeded, string Message, string? Handle, string? SessionToken, bool OtpRequired);
public sealed record CreatePostResult(bool Succeeded, string Message, Guid? Id, string? AuthorHandle);
public sealed record SimpleResult(bool Succeeded, string Message);
public sealed record UserProfileResult(bool Succeeded, string Message, string? Handle, string? DisplayName);
public sealed record RecentPostsResult(IReadOnlyList<PostSummaryResult> Posts);
public sealed record QuotedPostSummaryResult(Guid Id, string AuthorHandle, string Content);
public sealed record PostSummaryResult(
    Guid Id,
    string AuthorHandle,
    string Content,
    Guid? ParentPostId,
    Guid? OriginalPostId,
    int LikeCount,
    bool LikedByCurrentReader,
    int RepostCount,
    bool RepostedByCurrentReader,
    QuotedPostSummaryResult? QuotedPost);
