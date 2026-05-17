namespace SocialApp.Web.Services;

using System.Text.Json;
using Microsoft.JSInterop;

public sealed class AppSession(IJSRuntime js)
{
    private const string StorageKey = "socialapp-session";

    public string? Handle { get; private set; }
    public string? SessionToken { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Handle) && !string.IsNullOrWhiteSpace(SessionToken);

    public async Task RestoreAsync()
    {
        if (IsLoggedIn)
        {
            return;
        }

        var value = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var session = JsonSerializer.Deserialize<StoredSession>(value);
        if (string.IsNullOrWhiteSpace(session?.Handle) || string.IsNullOrWhiteSpace(session.SessionToken))
        {
            await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
            return;
        }

        Handle = session.Handle;
        SessionToken = session.SessionToken;
    }

    public async Task SignInAsync(string handle, string sessionToken)
    {
        Handle = handle;
        SessionToken = sessionToken;
        await js.InvokeVoidAsync(
            "localStorage.setItem",
            StorageKey,
            JsonSerializer.Serialize(new StoredSession(handle, sessionToken)));
    }

    public async Task SignOutAsync()
    {
        Handle = null;
        SessionToken = null;
        await js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
    }

    private sealed record StoredSession(string Handle, string SessionToken);
}
