using System.Text.Json;
using Azure.Storage.Blobs;
using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;
using Microsoft.Extensions.Options;
using SocialApp.Infrastructure.AzureBlobStorage.Options;

namespace SocialApp.Infrastructure.AzureBlobStorage.Gateways;

public sealed class AzureBlobPostMediaStorageGateway : IPostMediaStorageGateway
{
    private readonly BlobContainerClient container;

    public AzureBlobPostMediaStorageGateway(BlobServiceClient serviceClient, IOptions<AzureBlobMediaOptions> options)
    {
        container = serviceClient.GetBlobContainerClient(options.Value.PostMediaContainer);
    }

    public PostMediaUploadReservation ReserveUpload(ReservePostMediaUpload upload)
    {
        var assetId = Guid.NewGuid();
        var owner = upload.OwnerHandle.Trim().TrimStart('@');
        var storageKey = $"post-media/{owner}/{assetId:N}";
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
        MetadataBlob(assetId, "pending").Upload(BinaryData.FromString(JsonSerializer.Serialize(pending)), overwrite: true);
        return new(assetId, storageKey, $"/api/media/uploads/{assetId}");
    }

    public async Task StoreUploadAsync(Guid assetId, Stream content, CancellationToken cancellationToken = default)
    {
        if (!await MetadataBlob(assetId, "pending").ExistsAsync(cancellationToken))
        {
            throw new InvalidOperationException("Upload session was not found.");
        }

        await ContentBlob(assetId).UploadAsync(content, overwrite: true, cancellationToken);
    }

    public PostMediaItem? CompleteUpload(CompleteReservedPostMediaUpload upload)
    {
        var pending = ReadJson<ReservedPostMediaUpload>(MetadataBlob(upload.AssetId, "pending"));
        if (pending is null || !string.Equals(pending.OwnerHandle, upload.OwnerHandle, StringComparison.OrdinalIgnoreCase) ||
            !ContentBlob(upload.AssetId).Exists())
        {
            return null;
        }

        MetadataBlob(upload.AssetId, "completed").Upload(BinaryData.FromString(JsonSerializer.Serialize(pending)), overwrite: true);
        MetadataBlob(upload.AssetId, "pending").DeleteIfExists();
        return ToItem(pending);
    }

    public PostMediaItem? FindCompletedAsset(Guid assetId, string ownerHandle)
    {
        var completed = ReadJson<ReservedPostMediaUpload>(MetadataBlob(assetId, "completed"));
        return completed is not null &&
               string.Equals(completed.OwnerHandle, ownerHandle, StringComparison.OrdinalIgnoreCase) &&
               ContentBlob(assetId).Exists()
            ? ToItem(completed)
            : null;
    }

    public StoredPostMedia? FindStored(Guid assetId)
    {
        var completed = ReadJson<ReservedPostMediaUpload>(MetadataBlob(assetId, "completed"));
        var content = ContentBlob(assetId);
        if (completed is null || !content.Exists())
        {
            return null;
        }

        return new(completed.ContentType, content.OpenRead());
    }

    private BlobClient MetadataBlob(Guid assetId, string state) => container.GetBlobClient($"metadata/{assetId:N}.{state}.json");
    private BlobClient ContentBlob(Guid assetId) => container.GetBlobClient($"content/{assetId:N}");

    private static PostMediaItem ToItem(ReservedPostMediaUpload upload) =>
        new(upload.AssetId, upload.Kind, upload.StorageKey, upload.ContentType, upload.ByteLength, upload.Width, upload.Height, upload.DurationMs, 0, upload.ThumbnailKey, upload.AltText);

    private static T? ReadJson<T>(BlobClient blob)
    {
        if (!blob.Exists())
        {
            return default;
        }

        var download = blob.DownloadContent();
        return JsonSerializer.Deserialize<T>(download.Value.Content);
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
