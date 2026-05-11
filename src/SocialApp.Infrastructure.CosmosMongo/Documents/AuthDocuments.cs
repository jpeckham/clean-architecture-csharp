using MongoDB.Bson.Serialization.Attributes;

namespace SocialApp.Infrastructure.CosmosMongo.Documents;

public sealed class PendingRegistrationDocument
{
    [BsonId]
    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;
    public string Handle { get; init; } = string.Empty;
    [BsonElement("Password")]
    public string PasswordHash { get; init; } = string.Empty;
}

public sealed class VerificationCodeDocument
{
    [BsonId]
    public string Id { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
}

public sealed class RememberedDeviceDocument
{
    [BsonId]
    public string Id { get; init; } = string.Empty;

    public string Handle { get; init; } = string.Empty;
    public string DeviceId { get; init; } = string.Empty;
}

public sealed class PasswordResetTokenDocument
{
    [BsonId]
    public string Token { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public bool Used { get; init; }
}
