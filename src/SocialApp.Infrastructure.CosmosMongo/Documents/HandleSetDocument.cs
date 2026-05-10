using MongoDB.Bson.Serialization.Attributes;

namespace SocialApp.Infrastructure.CosmosMongo.Documents;

public sealed class HandleSetDocument
{
    [BsonId]
    public string ReaderHandle { get; init; } = string.Empty;

    public string[] Handles { get; init; } = Array.Empty<string>();
}
