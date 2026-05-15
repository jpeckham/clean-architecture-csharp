using System.Security.Cryptography;

namespace SocialApp.User.Gateways;

public interface IPasswordGateway
{
    string Hash(string password);
    bool Verify(string password, string storedPassword);
    string NormalizeStoredPassword(string storedPassword);
}

public sealed class Pbkdf2PasswordGateway : IPasswordGateway
{
    private const string Algorithm = "PBKDF2";
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        Validate(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string storedPassword)
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

    public string NormalizeStoredPassword(string storedPassword) =>
        storedPassword.StartsWith($"{Algorithm}$", StringComparison.Ordinal)
            ? storedPassword
            : Hash(storedPassword);

    private static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || !password.Any(char.IsDigit))
        {
            throw new ArgumentException("Password must be at least 8 characters and include a digit.", nameof(password));
        }
    }
}
