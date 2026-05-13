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

public interface IUserProfilePostGateway
{
    IReadOnlyList<UserProfilePostSummary> RecentPostsByAuthor(string authorHandle, string readerHandle, int limit);
}

public interface IProfileImageStorageGateway
{
    ProfileImageUploadReservation ReserveUpload(ReserveProfileImageUpload upload);
    ReservedProfileImageUpload? CompleteUpload(CompleteReservedProfileImageUpload upload);
    Task StoreUploadAsync(Guid assetId, Stream content, CancellationToken cancellationToken = default);
    StoredProfileImage? FindStored(Guid assetId);
    void Remove(Guid assetId);
}

public sealed record PendingRegistration(string DisplayName, string Handle, string Email, string PasswordHash);
public sealed record SentEmail(string To, string Subject, string Body);
public sealed record PasswordResetToken(string Email, DateTimeOffset ExpiresAt);
public sealed record ReserveProfileImageUpload(string OwnerHandle, string ContentType, long ByteLength);
public sealed record CompleteReservedProfileImageUpload(Guid AssetId, string OwnerHandle);
public sealed record ProfileImageUploadReservation(Guid AssetId, string StorageKey, string UploadUrl);
public sealed record ReservedProfileImageUpload(Guid AssetId, string OwnerHandle, string StorageKey, string ContentType, long ByteLength);
public sealed record StoredProfileImage(string ContentType, Stream Content);
public sealed record UserProfileQuotedPostSummary(Guid Id, string AuthorHandle, string Content, DateTimeOffset CreatedAt);
public sealed record UserProfilePostMediaSummary(
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
    string? MediaUrl = null);
public sealed record UserProfilePostSummary(
    Guid Id,
    string AuthorHandle,
    string Content,
    Guid? ParentPostId,
    Guid? OriginalPostId,
    DateTimeOffset CreatedAt,
    int LikeCount,
    bool LikedByCurrentReader,
    int RepostCount,
    bool RepostedByCurrentReader,
    UserProfileQuotedPostSummary? QuotedPost,
    IReadOnlyList<UserProfilePostMediaSummary>? Media = null);

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
        var existingIndex = _users.FindIndex(u => u.Id == user.Id);
        if (existingIndex >= 0)
        {
            _users[existingIndex] = user;
            return;
        }

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

public sealed class InMemoryProfileImageStorageGateway : IProfileImageStorageGateway
{
    private readonly Dictionary<Guid, ReservedProfileImageUpload> _pending = new();
    private readonly Dictionary<Guid, ReservedProfileImageUpload> _completed = new();

    public ProfileImageUploadReservation ReserveUpload(ReserveProfileImageUpload upload)
    {
        if (string.IsNullOrWhiteSpace(upload.OwnerHandle))
        {
            throw new ArgumentException("Owner handle is required.", nameof(upload));
        }

        if (string.IsNullOrWhiteSpace(upload.ContentType) ||
            !upload.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Profile image content type must be an image.", nameof(upload));
        }

        if (upload.ByteLength <= 0)
        {
            throw new ArgumentException("Byte length must be greater than zero.", nameof(upload));
        }

        var assetId = Guid.NewGuid();
        var storageKey = $"profile-images/{upload.OwnerHandle.Trim().TrimStart('@')}/{assetId}";
        _pending[assetId] = new ReservedProfileImageUpload(
            assetId,
            upload.OwnerHandle.Trim(),
            storageKey,
            upload.ContentType.Trim(),
            upload.ByteLength);
        return new(assetId, storageKey, $"profile-image://{assetId}");
    }

    public ReservedProfileImageUpload? CompleteUpload(CompleteReservedProfileImageUpload upload)
    {
        if (!_pending.TryGetValue(upload.AssetId, out var pending) ||
            !string.Equals(pending.OwnerHandle, upload.OwnerHandle, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        _pending.Remove(upload.AssetId);
        _completed[upload.AssetId] = pending;
        return pending;
    }

    public Task StoreUploadAsync(Guid assetId, Stream content, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Use a persistent profile image storage adapter for file content.");
    }

    public StoredProfileImage? FindStored(Guid assetId)
    {
        if (!_completed.TryGetValue(assetId, out var completed))
        {
            return null;
        }

        return new(completed.ContentType, Stream.Null);
    }

    public void Remove(Guid assetId)
    {
        _pending.Remove(assetId);
        _completed.Remove(assetId);
    }
}
