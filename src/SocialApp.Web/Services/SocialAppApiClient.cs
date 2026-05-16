using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;

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

    public async Task<BeginPostMediaUploadResult?> BeginPostMediaUploadAsync(string sessionToken, BeginPostMediaUploadRequest request)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/posts/media/upload-sessions")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        return await ReadAsync<BeginPostMediaUploadResult>(await http.SendAsync(message));
    }

    public async Task<SimpleResult?> StoreUploadAsync(string uploadUrl, IBrowserFile file, long maxAllowedSize)
    {
        await using var stream = file.OpenReadStream(maxAllowedSize);
        using var content = new StreamContent(stream);
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        }

        using var message = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
        {
            Content = content
        };
        using var response = await http.SendAsync(message);
        return response.IsSuccessStatusCode
            ? new SimpleResult(true, "Upload stored.")
            : await response.Content.ReadFromJsonAsync<SimpleResult>();
    }

    public async Task<CompletePostMediaUploadResult?> CompletePostMediaUploadAsync(string sessionToken, Guid assetId)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, $"/api/posts/media/{assetId}/complete")
        {
            Content = JsonContent.Create(new { })
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        return await ReadAsync<CompletePostMediaUploadResult>(await http.SendAsync(message));
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

    public async Task<RecentPostsResult?> GetRecentPostsAsync(string sessionToken, int limit = 20)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, $"/api/posts/recent?limit={limit}");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        using var response = await http.SendAsync(message);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<RecentPostsResult>()
            : null;
    }

    public async Task<RecentPostsResult?> SearchPostsAsync(string sessionToken, string query)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, $"/api/posts/search?query={Uri.EscapeDataString(query)}");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        using var response = await http.SendAsync(message);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<RecentPostsResult>()
            : null;
    }

    public async Task<UserProfileResult?> GetUserAsync(string sessionToken, string handle)
    {
        using var message = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{Uri.EscapeDataString(handle)}");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        return await ReadAsync<UserProfileResult>(await http.SendAsync(message));
    }

    public async Task<ProfileImageUploadResult?> UploadMyProfileImageAsync(string sessionToken, IBrowserFile image)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(image.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
        content.Add(fileContent, "image", image.Name);
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/profile-image")
        {
            Content = content
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        return await ReadAsync<ProfileImageUploadResult>(await http.SendAsync(message));
    }

    public async Task<SimpleResult?> RemoveMyProfileImageAsync(string sessionToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me/profile-image");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sessionToken);
        return await ReadAsync<SimpleResult>(await http.SendAsync(message));
    }

    public string ToAbsoluteApiUrl(string relativeUrl) =>
        new Uri(http.BaseAddress!, relativeUrl.TrimStart('/')).ToString();

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
public sealed record CreatePostRequest(string Content, IReadOnlyList<Guid>? MediaAssetIds = null);
public sealed record RepostPostRequest(string Content);
public sealed record BeginPostMediaUploadRequest(string Kind, string ContentType, long ByteLength, int? Width = null, int? Height = null, long? DurationMs = null, string? AltText = null);
public sealed record AuthResult(bool Succeeded, string Message, string? Handle, string? SessionToken);
public sealed record DeviceLoginResult(bool Succeeded, string Message, string? Handle, string? SessionToken, bool OtpRequired);
public sealed record CreatePostResult(bool Succeeded, string Message, Guid? Id, string? AuthorHandle);
public sealed record SimpleResult(bool Succeeded, string Message);
public sealed record BeginPostMediaUploadResult(bool Succeeded, string Message, Guid? AssetId, string? UploadUrl);
public sealed record CompletePostMediaUploadResult(bool Succeeded, string Message, Guid? AssetId);
public sealed record ProfileImageUploadResult(bool Succeeded, string Message, ProfileImageSummaryResult? ProfileImage);
public sealed record ProfileImageSummaryResult(Guid AssetId, string StorageKey, string ContentType, long ByteLength, int? Width, int? Height, DateTimeOffset UploadedAt, string ImageUrl);
public sealed record UserProfileResult(bool Succeeded, string Message, string? Handle, string? DisplayName, ProfileImageSummaryResult? ProfileImage, IReadOnlyList<PostSummaryResult> Posts);
public sealed record RecentPostsResult(IReadOnlyList<PostSummaryResult> Posts);
public sealed record QuotedPostSummaryResult(Guid Id, string AuthorHandle, string Content, DateTimeOffset CreatedAt, IReadOnlyList<PostMediaSummaryResult>? Media = null);
public sealed record PostMediaSummaryResult(
    Guid AssetId,
    string Kind,
    string ContentType,
    long ByteLength,
    int? Width,
    int? Height,
    long? DurationMs,
    int SortOrder,
    string? ThumbnailKey,
    string? AltText,
    string? MediaUrl);
public sealed record PostContentSegmentResult(int Sequence, string Text, string? MentionHandle);
public sealed record PostSummaryResult(
    Guid Id,
    string AuthorHandle,
    IReadOnlyList<PostContentSegmentResult> ContentSegments,
    Guid? ParentPostId,
    Guid? OriginalPostId,
    DateTimeOffset CreatedAt,
    int LikeCount,
    bool LikedByCurrentReader,
    int RepostCount,
    bool RepostedByCurrentReader,
    QuotedPostSummaryResult? QuotedPost,
    IReadOnlyList<PostMediaSummaryResult>? Media = null);
