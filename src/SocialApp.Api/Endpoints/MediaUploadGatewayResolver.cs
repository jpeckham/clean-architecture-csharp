using SocialApp.Post.Gateways;
using SocialApp.User.Gateways;

namespace SocialApp.Api.Endpoints;

public delegate Task StoreMediaUploadAsync(Guid assetId, Stream content, CancellationToken cancellationToken = default);

public interface IMediaUploadGatewayResolver
{
    StoreMediaUploadAsync? Resolve(string target);
}

public sealed class MediaUploadGatewayResolver(
    IProfileImageStorageGateway profileImages,
    IPostMediaStorageGateway postMedia) : IMediaUploadGatewayResolver
{
    public const string ProfileImagesTarget = "profile-images";
    public const string PostMediaTarget = "post-media";

    public StoreMediaUploadAsync? Resolve(string target) =>
        target.Trim().ToLowerInvariant() switch
        {
            ProfileImagesTarget => profileImages.StoreUploadAsync,
            PostMediaTarget => postMedia.StoreUploadAsync,
            _ => null
        };
}
