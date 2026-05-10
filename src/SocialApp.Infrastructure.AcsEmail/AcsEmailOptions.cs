namespace SocialApp.Infrastructure.AcsEmail;

public sealed class AcsEmailOptions
{
    public const string SectionName = "AcsEmail";

    public string ConnectionString { get; init; } = string.Empty;
    public string SenderAddress { get; init; } = string.Empty;
}
