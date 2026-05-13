using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SocialApp.Infrastructure.AzureBlobStorage.Gateways;
using SocialApp.Infrastructure.AzureBlobStorage.Options;
using SocialApp.Post.Gateways;
using SocialApp.User.Gateways;

namespace SocialApp.Infrastructure.AzureBlobStorage;

public static class DependencyInjection
{
    public static IServiceCollection AddAzureBlobMediaStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureBlobMediaOptions>(configuration.GetSection(AzureBlobMediaOptions.SectionName));
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureBlobMediaOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                return new BlobServiceClient(options.ConnectionString);
            }

            if (string.IsNullOrWhiteSpace(options.AccountUri))
            {
                throw new InvalidOperationException("AzureBlobMedia:ConnectionString or AzureBlobMedia:AccountUri is required.");
            }

            return new BlobServiceClient(new Uri(options.AccountUri), new DefaultAzureCredential());
        });

        services.AddSingleton<IProfileImageStorageGateway, AzureBlobProfileImageStorageGateway>();
        services.AddSingleton<IPostMediaStorageGateway, AzureBlobPostMediaStorageGateway>();
        return services;
    }
}
