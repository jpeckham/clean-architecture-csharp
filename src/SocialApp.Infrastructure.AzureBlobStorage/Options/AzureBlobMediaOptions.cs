namespace SocialApp.Infrastructure.AzureBlobStorage.Options;

public sealed class AzureBlobMediaOptions
{
    public const string SectionName = "AzureBlobMedia";
    public string? ConnectionString { get; set; }
    public string? AccountUri { get; set; }
    public string ProfileImagesContainer { get; set; } = "profile-images";
    public string PostMediaContainer { get; set; } = "post-media";
}
