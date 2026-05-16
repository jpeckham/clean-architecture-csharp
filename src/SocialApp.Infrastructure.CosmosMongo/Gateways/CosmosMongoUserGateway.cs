using MongoDB.Driver;
using SocialApp.Infrastructure.CosmosMongo.Documents;
using SocialApp.User.Entities;
using SocialApp.User.Gateways;

namespace SocialApp.Infrastructure.CosmosMongo.Gateways;

public sealed class CosmosMongoUserGateway(CosmosMongoCollections collections, ICredentialsGateway credentials) : IUserGateway
{
    private readonly IMongoCollection<UserDocument> _users = collections.Users;

    public UserAccount Save(string displayName, string handle, string email, string password)
    {
        var user = UserAccount.CreateWithCredentials(displayName, handle, email, credentials.Create(password));
        Save(user);
        return user;
    }

    public void Save(UserAccount user)
    {
        var existingById = _users.Find(u => u.Id == user.Id).FirstOrDefault();
        if (existingById is not null)
        {
            _users.ReplaceOne(u => u.Id == user.Id, CosmosMongoMappers.ToDocument(user));
            return;
        }

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

    public void ChangePassword(UserAccount user, string password)
    {
        user.ChangeCredentials(credentials.Create(password));
        Save(user);
    }

    public UserAccount? Authenticate(string email, string password)
    {
        var user = FindByEmail(email);
        return user is not null && credentials.Matches(password, user.Credentials)
            ? user
            : null;
    }

    public UserAccount? FindByHandle(string handle) =>
        _users.Find(u => u.Handle.ToLower() == UserAccount.NormalizeHandle(handle).ToLower() ||
                         u.Handle.ToLower() == $"@{UserAccount.NormalizeHandle(handle)}".ToLower()).FirstOrDefault() is { } document
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
