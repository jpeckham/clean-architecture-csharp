using System.Text.Json;
using Microsoft.Extensions.Options;
using SocialApp.Infrastructure.LocalStorage.Options;
using SocialApp.User.Gateways;

namespace SocialApp.Infrastructure.LocalStorage.Gateways;

public sealed class FileSystemProfileImageStorageGateway(IOptions<LocalMediaStorageOptions> options) : IProfileImageStorageGateway
{
    private readonly string rootPath = options.Value.RootPath;

    public ProfileImageUploadReservation ReserveUpload(ReserveProfileImageUpload upload)
    {
        if (string.IsNullOrWhiteSpace(upload.OwnerHandle))
        {
            throw new ArgumentException("Owner handle is required.", nameof(upload));
        }

        if (string.IsNullOrWhiteSpace(upload.ContentType) ||
            !upload.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Profile image content type must be an image.", nameof(upload));
        }

        if (upload.ByteLength <= 0)
        {
            throw new ArgumentException("Byte length must be greater than zero.", nameof(upload));
        }

        var assetId = Guid.NewGuid();
        var owner = CleanOwner(upload.OwnerHandle);
        var storageKey = $"profile-images/{owner}/{assetId}";
        var pending = new ReservedProfileImageUpload(assetId, upload.OwnerHandle.Trim(), storageKey, upload.ContentType.Trim(), upload.ByteLength);
        WriteJson(PendingPath(assetId), pending);
        return new(assetId, storageKey, $"/api/media/uploads/profile-images/{assetId}");
    }

    public ReservedProfileImageUpload? CompleteUpload(CompleteReservedProfileImageUpload upload)
    {
        var pending = ReadJson<ReservedProfileImageUpload>(PendingPath(upload.AssetId));
        if (pending is null || !string.Equals(pending.OwnerHandle, upload.OwnerHandle, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!File.Exists(ContentPath(upload.AssetId)))
        {
            return null;
        }

        File.Move(PendingPath(upload.AssetId), CompletedPath(upload.AssetId), overwrite: true);
        return pending;
    }

    public async Task StoreUploadAsync(Guid assetId, Stream content, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(PendingPath(assetId)))
        {
            throw new InvalidOperationException("Upload session was not found.");
        }

        Directory.CreateDirectory(UploadsPath());
        await using var file = File.Create(ContentPath(assetId));
        await content.CopyToAsync(file, cancellationToken);
        if (file.Length == 0)
        {
            throw new ArgumentException("Profile image content is required.", nameof(content));
        }
    }

    public StoredProfileImage? FindStored(Guid assetId)
    {
        var completed = ReadJson<ReservedProfileImageUpload>(CompletedPath(assetId));
        var contentPath = ContentPath(assetId);
        return completed is null || !File.Exists(contentPath)
            ? null
            : new(completed.ContentType, File.OpenRead(contentPath));
    }

    public void Remove(Guid assetId)
    {
        DeleteIfExists(PendingPath(assetId));
        DeleteIfExists(CompletedPath(assetId));
        DeleteIfExists(ContentPath(assetId));
    }

    private string MetadataPath() => Path.Combine(rootPath, "metadata", "profile-images");
    private string UploadsPath() => Path.Combine(rootPath, "content", "profile-images");
    private string PendingPath(Guid assetId) => Path.Combine(MetadataPath(), $"{assetId}.pending.json");
    private string CompletedPath(Guid assetId) => Path.Combine(MetadataPath(), $"{assetId}.completed.json");
    private string ContentPath(Guid assetId) => Path.Combine(UploadsPath(), assetId.ToString("N"));

    private static string CleanOwner(string ownerHandle) => ownerHandle.Trim().TrimStart('@');

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void WriteJson<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value));
    }

    private static T? ReadJson<T>(string path)
    {
        return File.Exists(path)
            ? JsonSerializer.Deserialize<T>(File.ReadAllText(path))
            : default;
    }
}
