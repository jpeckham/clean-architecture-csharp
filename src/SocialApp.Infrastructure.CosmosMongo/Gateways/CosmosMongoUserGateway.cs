using MongoDB.Driver;
using SocialApp.Infrastructure.CosmosMongo.Documents;
using SocialApp.User.Entities;
using SocialApp.User.Gateways;

namespace SocialApp.Infrastructure.CosmosMongo.Gateways;

public sealed class CosmosMongoUserGateway(CosmosMongoCollections collections) : IUserGateway
{
    private readonly IMongoCollection<UserDocument> _users = collections.Users;

    public void Save(UserAccount user)
    {
        if (FindByHandle(user.Handle) is not null)
        {
            throw new InvalidOperationException("Handle is already registered.");
        }

        if (FindByEmail(user.Email) is not null)
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        _users.InsertOne(CosmosMongoMappers.ToDocument(user));
    }

    public UserAccount? FindByHandle(string handle) =>
        _users.Find(u => u.Handle.ToLower() == handle.ToLower()).FirstOrDefault() is { } document
            ? CosmosMongoMappers.ToEntity(document)
            : null;

    public UserAccount? FindByEmail(string email) =>
        _users.Find(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault() is { } document
            ? CosmosMongoMappers.ToEntity(document)
            : null;

    public IReadOnlyList<UserAccount> Search(string query) =>
        _users.Find(u => u.Handle.ToLower().Contains(query.ToLower()) || u.DisplayName.ToLower().Contains(query.ToLower()))
            .ToList()
            .Select(CosmosMongoMappers.ToEntity)
            .ToArray();
}
