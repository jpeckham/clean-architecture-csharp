using System.Security.Cryptography;
using SocialApp.User.Gateways;

namespace SocialApp.Infrastructure.CosmosMongo.Gateways;

public sealed class Pbkdf2CredentialsGateway : ICredentialsGateway
{
    private const string Algorithm = "PBKDF2";
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string Create(string password)
    {
        Validate(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Matches(string password, string credentials)
    {
        if (!credentials.StartsWith($"{Algorithm}$", StringComparison.Ordinal))
        {
            return string.Equals(credentials, password, StringComparison.Ordinal);
        }

        var parts = credentials.Split('$');
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

    public string Normalize(string credentials) =>
        credentials.StartsWith($"{Algorithm}$", StringComparison.Ordinal)
            ? credentials
            : Create(credentials);

    private static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || !password.Any(char.IsDigit))
        {
            throw new ArgumentException("Password must be at least 8 characters and include a digit.", nameof(password));
        }
    }
}
