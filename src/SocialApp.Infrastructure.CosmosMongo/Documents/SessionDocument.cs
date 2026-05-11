using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SocialApp.Infrastructure.CosmosMongo.Documents;

public sealed class SessionDocument
{
    [BsonId]
    public string Token { get; init; } = string.Empty;

    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid UserId { get; init; }
    public string Handle { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}
