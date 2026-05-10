using MongoDB.Driver;
using SocialApp.Infrastructure.CosmosMongo.Documents;
using SocialApp.User.Entities;
using SocialApp.User.Gateways;

namespace SocialApp.Infrastructure.CosmosMongo.Gateways;

public sealed class CosmosMongoSessionGateway(
    CosmosMongoCollections collections,
    IUserGateway users) : ISessionGateway
{
    private readonly IMongoCollection<SessionDocument> _sessions = collections.Sessions;

    public string CreateSession(UserAccount user)
    {
        var token = $"session-{Guid.NewGuid():N}";
        _sessions.InsertOne(new SessionDocument
        {
            Token = token,
            UserId = user.Id,
            Handle = user.Handle,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return token;
    }

    public UserAccount? FindByToken(string token) =>
        _sessions.Find(s => s.Token == token).FirstOrDefault() is { } session
            ? users.FindByHandle(session.Handle)
            : null;
}
