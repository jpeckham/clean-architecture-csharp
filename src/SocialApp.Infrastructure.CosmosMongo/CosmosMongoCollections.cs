using MongoDB.Driver;
using SocialApp.Infrastructure.CosmosMongo.Documents;

namespace SocialApp.Infrastructure.CosmosMongo;

public sealed class CosmosMongoCollections(
    IMongoCollection<UserDocument> users,
    IMongoCollection<SessionDocument> sessions,
    IMongoCollection<PostDocument> posts,
    IMongoCollection<HandleSetDocument> follows,
    IMongoCollection<HandleSetDocument> blocks,
    IMongoCollection<PendingRegistrationDocument> pendingRegistrations,
    IMongoCollection<VerificationCodeDocument> verificationCodes,
    IMongoCollection<RememberedDeviceDocument> rememberedDevices,
    IMongoCollection<PasswordResetTokenDocument> passwordResetTokens)
{
    public IMongoCollection<UserDocument> Users { get; } = users;
    public IMongoCollection<SessionDocument> Sessions { get; } = sessions;
    public IMongoCollection<PostDocument> Posts { get; } = posts;
    public IMongoCollection<HandleSetDocument> Follows { get; } = follows;
    public IMongoCollection<HandleSetDocument> Blocks { get; } = blocks;
    public IMongoCollection<PendingRegistrationDocument> PendingRegistrations { get; } = pendingRegistrations;
    public IMongoCollection<VerificationCodeDocument> VerificationCodes { get; } = verificationCodes;
    public IMongoCollection<RememberedDeviceDocument> RememberedDevices { get; } = rememberedDevices;
    public IMongoCollection<PasswordResetTokenDocument> PasswordResetTokens { get; } = passwordResetTokens;
}
