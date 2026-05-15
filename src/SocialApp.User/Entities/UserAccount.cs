namespace SocialApp.User.Entities;

public sealed class UserAccount
{
    private string _passwordHash;

    private UserAccount(Guid id, string displayName, string handle, string email, string passwordHash, ProfileImage? profileImage)
    {
        Id = id;
        DisplayName = displayName;
        Handle = handle;
        Email = email;
        _passwordHash = passwordHash;
        ProfileImage = profileImage;
    }

    public Guid Id { get; }
    public string DisplayName { get; }
    public string Handle { get; }
    public string Email { get; }
    public string PasswordHash => _passwordHash;
    public ProfileImage? ProfileImage { get; private set; }

    public static UserAccount CreateWithPasswordHash(string displayName, string handle, string email, string passwordHash)
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

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        return new UserAccount(Guid.NewGuid(), displayName.Trim(), handle.Trim(), email.Trim(), passwordHash, null);
    }

    public static UserAccount Rehydrate(
        Guid id,
        string displayName,
        string handle,
        string email,
        string passwordHash,
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

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        return new UserAccount(id, displayName.Trim(), handle.Trim(), email.Trim(), passwordHash, profileImage);
    }

    public void ChangePasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        _passwordHash = passwordHash;
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
