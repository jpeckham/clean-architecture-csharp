using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SocialApp.Infrastructure.CosmosMongo.Documents;
using SocialApp.Infrastructure.CosmosMongo.Gateways;
using SocialApp.Infrastructure.CosmosMongo.Options;
using SocialApp.Post.Gateways;
using SocialApp.User.Gateways;

namespace SocialApp.Infrastructure.CosmosMongo;

public static class DependencyInjection
{
    public static IServiceCollection AddCosmosMongoInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CosmosMongoOptions>(configuration.GetSection(CosmosMongoOptions.SectionName));
        services.AddSingleton<IMongoDatabase>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<CosmosMongoOptions>>().Value;
            return new MongoClient(options.ConnectionString).GetDatabase(options.DatabaseName);
        });

        services.AddSingleton(provider =>
        {
            var database = provider.GetRequiredService<IMongoDatabase>();
            var options = provider.GetRequiredService<IOptions<CosmosMongoOptions>>().Value;
            return new CosmosMongoCollections(
                database.GetCollection<UserDocument>(options.UsersCollectionName),
                database.GetCollection<SessionDocument>(options.SessionsCollectionName),
                database.GetCollection<PostDocument>(options.PostsCollectionName),
                database.GetCollection<HandleSetDocument>(options.FollowsCollectionName),
                database.GetCollection<HandleSetDocument>(options.BlocksCollectionName),
                database.GetCollection<PendingRegistrationDocument>(options.PendingRegistrationsCollectionName),
                database.GetCollection<VerificationCodeDocument>(options.VerificationCodesCollectionName),
                database.GetCollection<RememberedDeviceDocument>(options.RememberedDevicesCollectionName),
                database.GetCollection<PasswordResetTokenDocument>(options.PasswordResetTokensCollectionName));
        });

        services.AddSingleton<IAccountHandleGateway, CosmosMongoAccountHandleGateway>();
        services.AddSingleton<IUserGateway, CosmosMongoUserGateway>();
        services.AddSingleton<ICredentialsGateway, Pbkdf2CredentialsGateway>();
        services.AddSingleton<ISessionGateway, CosmosMongoSessionGateway>();
        services.AddSingleton<IPostGateway, CosmosMongoPostGateway>();
        services.AddSingleton<IPostSearchGateway, CosmosMongoPostGateway>();
        services.AddSingleton<IPendingRegistrationGateway, CosmosMongoPendingRegistrationGateway>();
        services.AddSingleton<IVerificationCodeGateway, CosmosMongoVerificationCodeGateway>();
        services.AddSingleton<IRememberedDeviceGateway, CosmosMongoRememberedDeviceGateway>();
        services.AddSingleton<IPasswordResetTokenGateway, CosmosMongoPasswordResetTokenGateway>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }

}
