using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SocialApp.User.Gateways;

namespace SocialApp.Infrastructure.AcsEmail;

public static class DependencyInjection
{
    public static IServiceCollection AddAcsEmailInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AcsEmailOptions>(configuration.GetSection(AcsEmailOptions.SectionName));
        services.AddSingleton<IEmailGateway, AcsEmailGateway>();
        return services;
    }
}
