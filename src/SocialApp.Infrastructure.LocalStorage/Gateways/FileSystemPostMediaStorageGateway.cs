using System.Text.Json;
using Microsoft.Extensions.Options;
using SocialApp.Infrastructure.LocalStorage.Options;
using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;

namespace SocialApp.Infrastructure.LocalStorage.Gateways;

public sealed class FileSystemPostMediaStorageGateway(IOptions<LocalMediaStorageOptions> options) : IPostMediaStorageGateway
{
    private readonly string rootPath = options.Value.RootPath;

    public PostMediaUploadReservation ReserveUpload(ReservePostMediaUpload upload)
    {
        if (string.IsNullOrWhiteSpace(upload.OwnerHandle))
        {
            throw new ArgumentException("Owner handle is required.", nameof(upload));
        }

        var assetId = Guid.NewGuid();
        var owner = upload.OwnerHandle.Trim().TrimStart('@');
        var storageKey = $"post-media/{owner}/{assetId}";
        var pending = new ReservedPostMediaUpload(
            assetId,
            upload.OwnerHandle.Trim(),
            upload.Kind,
            storageKey,
            upload.ContentType.Trim(),
            upload.ByteLength,
            upload.Width,
            upload.Height,
            upload.DurationMs,
            upload.ThumbnailKey,
            upload.AltText);
        WriteJson(PendingPath(assetId), pending);
        return new(assetId, storageKey, $"/api/media/uploads/post-media/{assetId}");
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
            throw new ArgumentException("Post media content is required.", nameof(content));
        }
    }

    public PostMediaItem? CompleteUpload(CompleteReservedPostMediaUpload upload)
    {
        var pending = ReadJson<ReservedPostMediaUpload>(PendingPath(upload.AssetId));
        if (pending is null || !string.Equals(pending.OwnerHandle, upload.OwnerHandle, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!File.Exists(ContentPath(upload.AssetId)))
        {
            return null;
        }

        var item = ToItem(pending);
        WriteJson(CompletedPath(upload.AssetId), pending);
        File.Delete(PendingPath(upload.AssetId));
        return item;
    }

    public PostMediaItem? FindCompletedAsset(Guid assetId, string ownerHandle)
    {
        var completed = ReadJson<ReservedPostMediaUpload>(CompletedPath(assetId));
        return completed is not null &&
               string.Equals(completed.OwnerHandle, ownerHandle, StringComparison.OrdinalIgnoreCase) &&
               File.Exists(ContentPath(assetId))
            ? ToItem(completed)
            : null;
    }

    public StoredPostMedia? FindStored(Guid assetId)
    {
        var completed = ReadJson<ReservedPostMediaUpload>(CompletedPath(assetId));
        var contentPath = ContentPath(assetId);
        return completed is null || !File.Exists(contentPath)
            ? null
            : new(completed.ContentType, File.OpenRead(contentPath));
    }

    private string MetadataPath() => Path.Combine(rootPath, "metadata", "post-media");
    private string UploadsPath() => Path.Combine(rootPath, "content", "post-media");
    private string PendingPath(Guid assetId) => Path.Combine(MetadataPath(), $"{assetId}.pending.json");
    private string CompletedPath(Guid assetId) => Path.Combine(MetadataPath(), $"{assetId}.completed.json");
    private string ContentPath(Guid assetId) => Path.Combine(UploadsPath(), assetId.ToString("N"));

    private static PostMediaItem ToItem(ReservedPostMediaUpload upload) =>
        new(
            upload.AssetId,
            upload.Kind,
            upload.StorageKey,
            upload.ContentType,
            upload.ByteLength,
            upload.Width,
            upload.Height,
            upload.DurationMs,
            0,
            upload.ThumbnailKey,
            upload.AltText);

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

    private sealed record ReservedPostMediaUpload(
        Guid AssetId,
        string OwnerHandle,
        PostMediaKind Kind,
        string StorageKey,
        string ContentType,
        long ByteLength,
        int? Width,
        int? Height,
        long? DurationMs,
        string? ThumbnailKey,
        string? AltText);
}
