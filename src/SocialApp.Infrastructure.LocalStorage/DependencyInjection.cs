using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialApp.Infrastructure.LocalStorage.Gateways;
using SocialApp.Infrastructure.LocalStorage.Options;
using SocialApp.Post.Gateways;
using SocialApp.User.Gateways;

namespace SocialApp.Infrastructure.LocalStorage;

public static class DependencyInjection
{
    public static IServiceCollection AddLocalMediaStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LocalMediaStorageOptions>(configuration.GetSection(LocalMediaStorageOptions.SectionName));
        services.AddSingleton<IProfileImageStorageGateway, FileSystemProfileImageStorageGateway>();
        services.AddSingleton<IPostMediaStorageGateway, FileSystemPostMediaStorageGateway>();
        return services;
    }
}
