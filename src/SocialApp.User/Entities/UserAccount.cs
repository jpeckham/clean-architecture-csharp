using System.Security.Cryptography;

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
        return new UserAccount(Guid.NewGuid(), displayName.Trim(), handle.Trim(), email.Trim(), PasswordHasher.Hash(password), null);
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

    public static UserAccount CreateWithPasswordHash(string displayName, string handle, string email, string passwordHash)
    {
        return Rehydrate(Guid.NewGuid(), displayName, handle, email, PasswordHasher.NormalizeStoredPassword(passwordHash));
    }

    public bool CheckPassword(string password) => PasswordHasher.Verify(password, _passwordHash);

    public void ChangePassword(string password)
    {
        PasswordPolicy.Validate(password);
        _passwordHash = PasswordHasher.Hash(password);
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

internal static class PasswordHasher
{
    private const string Algorithm = "PBKDF2";
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedPassword)
    {
        if (!storedPassword.StartsWith($"{Algorithm}$", StringComparison.Ordinal))
        {
            return string.Equals(storedPassword, password, StringComparison.Ordinal);
        }

        var parts = storedPassword.Split('$');
        if (parts.Length != 4 ||
            !int.TryParse(parts[1], out var iterations) ||
            iterations <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expectedHash = Convert.FromBase64String(parts[3]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string NormalizeStoredPassword(string storedPassword) =>
        storedPassword.StartsWith($"{Algorithm}$", StringComparison.Ordinal)
            ? storedPassword
            : Hash(storedPassword);
}
