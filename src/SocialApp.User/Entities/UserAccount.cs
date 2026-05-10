namespace SocialApp.User.Entities;

public sealed class UserAccount
{
    private string _password;

    private UserAccount(Guid id, string displayName, string handle, string email, string password)
    {
        Id = id;
        DisplayName = displayName;
        Handle = handle;
        Email = email;
        _password = password;
    }

    public Guid Id { get; }
    public string DisplayName { get; }
    public string Handle { get; }
    public string Email { get; }
    public string PasswordHash => _password;

    public static UserAccount Create(string displayName, string handle, string email, string password)
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

        PasswordPolicy.Validate(password);
        return new UserAccount(Guid.NewGuid(), displayName.Trim(), handle.Trim(), email.Trim(), password);
    }

    public static UserAccount Rehydrate(Guid id, string displayName, string handle, string email, string password)
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

        PasswordPolicy.Validate(password);
        return new UserAccount(id, displayName.Trim(), handle.Trim(), email.Trim(), password);
    }

    public bool CheckPassword(string password) => _password == password;

    public void ChangePassword(string password)
    {
        PasswordPolicy.Validate(password);
        _password = password;
    }
}

public static class PasswordPolicy
{
    public static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || !password.Any(char.IsDigit))
        {
            throw new ArgumentException("Password must be at least 8 characters and include a digit.", nameof(password));
        }
    }
}
