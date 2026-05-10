namespace SocialApp.Infrastructure.CosmosMongo.Options;

public sealed class CosmosMongoOptions
{
    public const string SectionName = "CosmosMongo";

    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = "socialapp";
    public string UsersCollectionName { get; init; } = "users";
    public string SessionsCollectionName { get; init; } = "sessions";
    public string PostsCollectionName { get; init; } = "posts";
    public string FollowsCollectionName { get; init; } = "postFollows";
    public string BlocksCollectionName { get; init; } = "postBlocks";
    public string PendingRegistrationsCollectionName { get; init; } = "pendingRegistrations";
    public string VerificationCodesCollectionName { get; init; } = "verificationCodes";
    public string RememberedDevicesCollectionName { get; init; } = "rememberedDevices";
    public string PasswordResetTokensCollectionName { get; init; } = "passwordResetTokens";
}
