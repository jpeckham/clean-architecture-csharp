using MongoDB.Bson.Serialization.Attributes;

namespace SocialApp.Infrastructure.CosmosMongo.Documents;

public sealed class PostDocument
{
    [BsonId]
    public Guid Id { get; init; }

    public string AuthorHandle { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public Guid? ParentPostId { get; init; }
    public Guid? OriginalPostId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public bool IsDeleted { get; init; }
    public string[] LikedBy { get; init; } = Array.Empty<string>();
}
