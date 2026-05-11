using SocialApp.User.Entities;

namespace SocialApp.User.Gateways;

public interface IUserGateway
{
    void Save(UserAccount user);
    UserAccount? FindByHandle(string handle);
    UserAccount? FindByEmail(string email);
    IReadOnlyList<UserAccount> Search(string query);
}

public interface ISessionGateway
{
    string CreateSession(UserAccount user);
    UserAccount? FindByToken(string token);
}

public interface IPasswordResetGateway
{
    string CreateResetToken(UserAccount user);
    string? FindToken(string email);
    UserAccount? FindByToken(string token);
    void Consume(string token);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface IEmailGateway
{
    void Send(string to, string subject, string body);
}

public interface IPendingRegistrationGateway
{
    void Save(PendingRegistration registration);
    PendingRegistration? FindByEmail(string email);
    void Remove(string email);
}

public interface IVerificationCodeGateway
{
    string CreateCode(string email, string purpose, TimeSpan lifetime);
    bool Verify(string email, string purpose, string code);
    string? FindActiveCode(string email);
}

public interface IRememberedDeviceGateway
{
    bool IsRemembered(string handle, string deviceId);
    void Remember(string handle, string deviceId);
}

public interface IPasswordResetTokenGateway
{
    string CreateToken(string email, TimeSpan lifetime);
    PasswordResetToken? Consume(string token);
    string? FindActiveToken(string email);
}

public sealed record PendingRegistration(string DisplayName, string Handle, string Email, string PasswordHash);
public sealed record SentEmail(string To, string Subject, string Body);
public sealed record PasswordResetToken(string Email, DateTimeOffset ExpiresAt);

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class MutableClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = utcNow;
    public void Advance(TimeSpan interval) => UtcNow = UtcNow.Add(interval);
}

public sealed class InMemoryUserGateway : IUserGateway
{
    private readonly List<UserAccount> _users = new();

    public IReadOnlyList<UserAccount> AllUsers => _users;

    public void Save(UserAccount user)
    {
        if (_users.Any(u => string.Equals(u.Handle, user.Handle, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Handle is already registered.");
        }

        if (_users.Any(u => string.Equals(u.Email, user.Email, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        _users.Add(user);
    }

    public UserAccount? FindByHandle(string handle) =>
        _users.SingleOrDefault(u => string.Equals(u.Handle, handle, StringComparison.OrdinalIgnoreCase));

    public UserAccount? FindByEmail(string email) =>
        _users.SingleOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<UserAccount> Search(string query) =>
        _users.Where(u => u.Handle.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                          u.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToArray();
}

public sealed class InMemorySessionGateway : ISessionGateway
{
    private readonly Dictionary<string, UserAccount> _sessions = new();

    public IReadOnlyDictionary<string, UserAccount> AllSessions => _sessions;

    public string CreateSession(UserAccount user)
    {
        var token = $"session-{Guid.NewGuid():N}";
        _sessions[token] = user;
        return token;
    }

    public UserAccount? FindByToken(string token) => _sessions.GetValueOrDefault(token);
}

public sealed class InMemoryPasswordResetGateway : IPasswordResetGateway
{
    private readonly Dictionary<string, UserAccount> _tokens = new();

    public string CreateResetToken(UserAccount user)
    {
        var token = $"reset-{Guid.NewGuid():N}";
        _tokens[token] = user;
        return token;
    }

    public string? FindToken(string email) =>
        _tokens.SingleOrDefault(pair => string.Equals(pair.Value.Email, email, StringComparison.OrdinalIgnoreCase)).Key;

    public UserAccount? FindByToken(string token) => _tokens.GetValueOrDefault(token);

    public void Consume(string token) => _tokens.Remove(token);
}

public sealed class InMemoryEmailGateway : IEmailGateway
{
    private readonly List<SentEmail> _sent = new();
    public IReadOnlyList<SentEmail> Sent => _sent;
    public void Send(string to, string subject, string body) => _sent.Add(new(to, subject, body));
}

public sealed class InMemoryPendingRegistrationGateway : IPendingRegistrationGateway
{
    private readonly Dictionary<string, PendingRegistration> _registrations = new(StringComparer.OrdinalIgnoreCase);
    public void Save(PendingRegistration registration) => _registrations[registration.Email] = registration;
    public PendingRegistration? FindByEmail(string email) => _registrations.GetValueOrDefault(email);
    public void Remove(string email) => _registrations.Remove(email);
}

public sealed class InMemoryVerificationCodeGateway(IClock? clock = null) : IVerificationCodeGateway
{
    private readonly IClock _clock = clock ?? new SystemClock();
    private readonly Dictionary<(string Email, string Purpose), (string Code, DateTimeOffset ExpiresAt)> _codes = new();

    public string CreateCode(string email, string purpose, TimeSpan lifetime)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        _codes[(email, purpose)] = (code, _clock.UtcNow.Add(lifetime));
        return code;
    }

    public bool Verify(string email, string purpose, string code)
    {
        if (!_codes.TryGetValue((email, purpose), out var value) || value.ExpiresAt < _clock.UtcNow || value.Code != code)
        {
            return false;
        }

        _codes.Remove((email, purpose));
        return true;
    }

    public string? FindActiveCode(string email) =>
        _codes.FirstOrDefault(pair => string.Equals(pair.Key.Email, email, StringComparison.OrdinalIgnoreCase) && pair.Value.ExpiresAt >= _clock.UtcNow).Value.Code;
}

public sealed class InMemoryRememberedDeviceGateway : IRememberedDeviceGateway
{
    private readonly HashSet<string> _remembered = new(StringComparer.OrdinalIgnoreCase);
    public bool IsRemembered(string handle, string deviceId) => _remembered.Contains($"{handle}|{deviceId}");
    public void Remember(string handle, string deviceId) => _remembered.Add($"{handle}|{deviceId}");
}

public sealed class InMemoryPasswordResetTokenGateway(IClock clock) : IPasswordResetTokenGateway
{
    private readonly Dictionary<string, PasswordResetToken> _tokens = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);

    public string CreateToken(string email, TimeSpan lifetime)
    {
        var token = $"reset-{Guid.NewGuid():N}";
        _tokens[token] = new PasswordResetToken(email, clock.UtcNow.Add(lifetime));
        return token;
    }

    public PasswordResetToken? Consume(string token)
    {
        if (_used.Contains(token) || !_tokens.TryGetValue(token, out var value) || value.ExpiresAt < clock.UtcNow)
        {
            return null;
        }

        _used.Add(token);
        _tokens.Remove(token);
        return value;
    }

    public string? FindActiveToken(string email) =>
        _tokens.FirstOrDefault(pair => string.Equals(pair.Value.Email, email, StringComparison.OrdinalIgnoreCase) && pair.Value.ExpiresAt >= clock.UtcNow).Key;
}
