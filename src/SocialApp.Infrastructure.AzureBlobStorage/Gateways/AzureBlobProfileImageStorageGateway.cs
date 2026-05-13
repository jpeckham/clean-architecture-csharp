using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using SocialApp.Infrastructure.AzureBlobStorage.Options;
using SocialApp.User.Gateways;

namespace SocialApp.Infrastructure.AzureBlobStorage.Gateways;

public sealed class AzureBlobProfileImageStorageGateway : IProfileImageStorageGateway
{
    private readonly BlobContainerClient container;

    public AzureBlobProfileImageStorageGateway(BlobServiceClient serviceClient, IOptions<AzureBlobMediaOptions> options)
    {
        container = serviceClient.GetBlobContainerClient(options.Value.ProfileImagesContainer);
    }

    public ProfileImageUploadReservation ReserveUpload(ReserveProfileImageUpload upload)
    {
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
        var owner = upload.OwnerHandle.Trim().TrimStart('@');
        var storageKey = $"profile-images/{owner}/{assetId:N}";
        var pending = new ReservedProfileImageUpload(assetId, upload.OwnerHandle.Trim(), storageKey, upload.ContentType.Trim(), upload.ByteLength);
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

    public ReservedProfileImageUpload? CompleteUpload(CompleteReservedProfileImageUpload upload)
    {
        var pending = ReadJson<ReservedProfileImageUpload>(MetadataBlob(upload.AssetId, "pending"));
        if (pending is null || !string.Equals(pending.OwnerHandle, upload.OwnerHandle, StringComparison.OrdinalIgnoreCase) ||
            !ContentBlob(upload.AssetId).Exists())
        {
            return null;
        }

        MetadataBlob(upload.AssetId, "completed").Upload(BinaryData.FromString(JsonSerializer.Serialize(pending)), overwrite: true);
        MetadataBlob(upload.AssetId, "pending").DeleteIfExists();
        return pending;
    }

    public StoredProfileImage? FindStored(Guid assetId)
    {
        var completed = ReadJson<ReservedProfileImageUpload>(MetadataBlob(assetId, "completed"));
        if (completed is null || !ContentBlob(assetId).Exists())
        {
            return null;
        }

        return new(completed.ContentType, ContentBlob(assetId).OpenRead());
    }

    public void Remove(Guid assetId)
    {
        MetadataBlob(assetId, "pending").DeleteIfExists();
        MetadataBlob(assetId, "completed").DeleteIfExists();
        ContentBlob(assetId).DeleteIfExists();
    }

    private BlobClient MetadataBlob(Guid assetId, string state) => container.GetBlobClient($"metadata/{assetId:N}.{state}.json");
    private BlobClient ContentBlob(Guid assetId) => container.GetBlobClient($"content/{assetId:N}");

    private static T? ReadJson<T>(BlobClient blob)
    {
        if (!blob.Exists())
        {
            return default;
        }

        var download = blob.DownloadContent();
        return JsonSerializer.Deserialize<T>(download.Value.Content);
    }
}
