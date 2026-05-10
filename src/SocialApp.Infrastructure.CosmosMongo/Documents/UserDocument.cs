using MongoDB.Bson.Serialization.Attributes;

namespace SocialApp.Infrastructure.CosmosMongo.Documents;

public sealed class UserDocument
{
    [BsonId]
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;
    public string Handle { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
