using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SocialApp.Infrastructure.CosmosMongo.Documents;

public sealed class PostDocument
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; init; }

    public string AuthorHandle { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid? ParentPostId { get; init; }
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid? OriginalPostId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public bool IsDeleted { get; init; }
    public string[] LikedBy { get; init; } = Array.Empty<string>();
    public PostMediaDocument[] Media { get; init; } = Array.Empty<PostMediaDocument>();
    public string[] Mentions { get; init; } = Array.Empty<string>();
}

public sealed class PostMediaDocument
{
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid AssetId { get; init; }

    public string Kind { get; init; } = string.Empty;
    public string StorageKey { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long ByteLength { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public long? DurationMs { get; init; }
    public int SortOrder { get; init; }
    public string? ThumbnailKey { get; init; }
    public string? AltText { get; init; }
}
