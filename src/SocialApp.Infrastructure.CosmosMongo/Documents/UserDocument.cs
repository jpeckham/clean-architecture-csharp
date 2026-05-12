using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SocialApp.Infrastructure.CosmosMongo.Documents;

public sealed class UserDocument
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;
    public string Handle { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    [BsonElement("Password")]
    public string PasswordHash { get; init; } = string.Empty;
    public ProfileImageDocument? ProfileImage { get; init; }
}

public sealed class ProfileImageDocument
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid AssetId { get; init; }
    public string StorageKey { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long ByteLength { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}
