namespace SocialApp.Infrastructure.LocalStorage.Options;

public sealed class LocalMediaStorageOptions
{
    public const string SectionName = "LocalMedia";
    public string RootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "media");
}
