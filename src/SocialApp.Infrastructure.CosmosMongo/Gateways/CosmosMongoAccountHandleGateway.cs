using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using SocialApp.Infrastructure.CosmosMongo.Documents;
using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;

namespace SocialApp.Infrastructure.CosmosMongo.Gateways;

public sealed class CosmosMongoAccountHandleGateway(CosmosMongoCollections collections) : IAccountHandleGateway
{
    private readonly IMongoCollection<UserDocument> _users = collections.Users;

    public bool Exists(string handle)
    {
        var normalized = SocialPost.NormalizeHandle(handle);
        var filter = Builders<UserDocument>.Filter.Regex(
            "Handle",
            new BsonRegularExpression($"^@?{Regex.Escape(normalized)}$", "i"));
        return _users.Find(filter).Any();
    }
}
