namespace SocialApp.User.Entities;

public sealed class UserAccount
{
    private string _credentials;

    private UserAccount(Guid id, string displayName, string handle, string email, string credentials, ProfileImage? profileImage)
    {
        Id = id;
        DisplayName = displayName;
        Handle = handle;
        Email = email;
        _credentials = credentials;
        ProfileImage = profileImage;
    }

    public Guid Id { get; }
    public string DisplayName { get; }
    public string Handle { get; }
    public string Email { get; }
    public string Credentials => _credentials;
    public ProfileImage? ProfileImage { get; private set; }

    public static UserAccount CreateWithCredentials(string displayName, string handle, string email, string credentials)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(handle) || !handle.StartsWith('@'))
        {
            throw new ArgumentException("Handle must start with @.", nameof(handle));
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ArgumentException("Email is invalid.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(credentials))
        {
            throw new ArgumentException("Credentials are required.", nameof(credentials));
        }

        return new UserAccount(Guid.NewGuid(), displayName.Trim(), handle.Trim(), email.Trim(), credentials, null);
    }

    public static UserAccount Rehydrate(
        Guid id,
        string displayName,
        string handle,
        string email,
        string credentials,
        ProfileImage? profileImage = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(handle) || !handle.StartsWith('@'))
        {
            throw new ArgumentException("Handle must start with @.", nameof(handle));
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            throw new ArgumentException("Email is invalid.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(credentials))
        {
            throw new ArgumentException("Credentials are required.", nameof(credentials));
        }

        return new UserAccount(id, displayName.Trim(), handle.Trim(), email.Trim(), credentials, profileImage);
    }

    public void ChangeCredentials(string credentials)
    {
        if (string.IsNullOrWhiteSpace(credentials))
        {
            throw new ArgumentException("Credentials are required.", nameof(credentials));
        }

        _credentials = credentials;
    }

    public void SetProfileImage(ProfileImage profileImage)
    {
        ProfileImage = profileImage ?? throw new ArgumentNullException(nameof(profileImage));
    }

    public void RemoveProfileImage()
    {
        ProfileImage = null;
    }
}
