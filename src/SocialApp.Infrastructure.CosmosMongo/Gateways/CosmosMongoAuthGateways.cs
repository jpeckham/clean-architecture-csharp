using MongoDB.Driver;
using SocialApp.Infrastructure.CosmosMongo.Documents;
using SocialApp.User.Entities;
using SocialApp.User.Gateways;

namespace SocialApp.Infrastructure.CosmosMongo.Gateways;

public sealed class CosmosMongoPendingRegistrationGateway(CosmosMongoCollections collections, ICredentialsGateway credentials) : IPendingRegistrationGateway
{
    private readonly IMongoCollection<PendingRegistrationDocument> _registrations = collections.PendingRegistrations;

    public void Save(string displayName, string handle, string email, string password)
    {
        var storedCredentials = credentials.Create(password);
        _registrations.ReplaceOne(
            r => r.Email == email,
            new PendingRegistrationDocument
            {
                Email = email,
                DisplayName = displayName,
                Handle = UserAccount.NormalizeHandle(handle),
                PasswordHash = storedCredentials
            },
            new ReplaceOptions { IsUpsert = true });
    }

    public PendingRegistration? FindByEmail(string email) =>
        _registrations.Find(r => r.Email == email).FirstOrDefault() is { } document
            ? new PendingRegistration(document.DisplayName, document.Handle, document.Email, credentials.Normalize(document.PasswordHash))
            : null;

    public void Remove(string email) => _registrations.DeleteOne(r => r.Email == email);
}

public sealed class CosmosMongoVerificationCodeGateway(CosmosMongoCollections collections, IClock clock) : IVerificationCodeGateway
{
    private readonly IMongoCollection<VerificationCodeDocument> _codes = collections.VerificationCodes;

    public string CreateCode(string email, string purpose, TimeSpan lifetime)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        var id = Id(email, purpose);
        _codes.ReplaceOne(
            c => c.Id == id,
            new VerificationCodeDocument { Id = id, Email = email, Purpose = purpose, Code = code, ExpiresAt = clock.UtcNow.Add(lifetime) },
            new ReplaceOptions { IsUpsert = true });
        return code;
    }

    public bool Verify(string email, string purpose, string code)
    {
        var id = Id(email, purpose);
        var document = _codes.Find(c => c.Id == id).FirstOrDefault();
        if (document is null || document.ExpiresAt < clock.UtcNow || document.Code != NormalizeCode(code))
        {
            return false;
        }

        _codes.DeleteOne(c => c.Id == id);
        return true;
    }

    public string? FindActiveCode(string email) =>
        _codes.Find(c => c.Email == email && c.ExpiresAt >= clock.UtcNow).FirstOrDefault()?.Code;

    private static string Id(string email, string purpose) => $"{email}|{purpose}".ToLowerInvariant();

    private static string NormalizeCode(string code)
    {
        var digits = new string(code.Where(char.IsDigit).ToArray());
        return digits.Length == 6 ? digits : code.Trim();
    }
}

public sealed class CosmosMongoRememberedDeviceGateway(CosmosMongoCollections collections) : IRememberedDeviceGateway
{
    private readonly IMongoCollection<RememberedDeviceDocument> _devices = collections.RememberedDevices;

    public bool IsRemembered(string handle, string deviceId) => _devices.Find(d => d.Id == Id(handle, deviceId)).Any();

    public void Remember(string handle, string deviceId) =>
        _devices.ReplaceOne(
            d => d.Id == Id(handle, deviceId),
            new RememberedDeviceDocument { Id = Id(handle, deviceId), Handle = handle, DeviceId = deviceId },
            new ReplaceOptions { IsUpsert = true });

    private static string Id(string handle, string deviceId) => $"{handle}|{deviceId}".ToLowerInvariant();
}

public sealed class CosmosMongoPasswordResetTokenGateway(CosmosMongoCollections collections, IClock clock) : IPasswordResetTokenGateway
{
    private readonly IMongoCollection<PasswordResetTokenDocument> _tokens = collections.PasswordResetTokens;

    public string CreateToken(string email, TimeSpan lifetime)
    {
        var token = $"reset-{Guid.NewGuid():N}";
        _tokens.InsertOne(new PasswordResetTokenDocument { Token = token, Email = email, ExpiresAt = clock.UtcNow.Add(lifetime), Used = false });
        return token;
    }

    public PasswordResetToken? Consume(string token)
    {
        var document = _tokens.Find(t => t.Token == token && !t.Used).FirstOrDefault();
        if (document is null || document.ExpiresAt < clock.UtcNow)
        {
            return null;
        }

        _tokens.ReplaceOne(t => t.Token == token, new PasswordResetTokenDocument
        {
            Token = document.Token,
            Email = document.Email,
            ExpiresAt = document.ExpiresAt,
            Used = true
        });
        return new PasswordResetToken(document.Email, document.ExpiresAt);
    }

    public string? FindActiveToken(string email) =>
        _tokens.Find(t => t.Email == email && !t.Used && t.ExpiresAt >= clock.UtcNow).FirstOrDefault()?.Token;
}
