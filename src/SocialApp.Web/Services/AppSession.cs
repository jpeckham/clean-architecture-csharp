namespace SocialApp.Web.Services;

public sealed class AppSession
{
    public string? Handle { get; private set; }
    public string? SessionToken { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrWhiteSpace(Handle) && !string.IsNullOrWhiteSpace(SessionToken);

    public void SignIn(string handle, string sessionToken)
    {
        Handle = handle;
        SessionToken = sessionToken;
    }

    public void SignOut()
    {
        Handle = null;
        SessionToken = null;
    }
}
