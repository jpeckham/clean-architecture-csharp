namespace SocialApp.User.Entities;

public sealed class ProfileImage
{
    public ProfileImage(
        Guid assetId,
        string storageKey,
        string contentType,
        long byteLength,
        int? width,
        int? height,
        DateTimeOffset uploadedAt)
    {
        if (assetId == Guid.Empty)
        {
            throw new ArgumentException("Asset id is required.", nameof(assetId));
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key is required.", nameof(storageKey));
        }

        if (string.IsNullOrWhiteSpace(contentType) ||
            !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Profile image content type must be an image.", nameof(contentType));
        }

        if (byteLength <= 0)
        {
            throw new ArgumentException("Byte length must be greater than zero.", nameof(byteLength));
        }

        if (width is <= 0)
        {
            throw new ArgumentException("Width must be greater than zero.", nameof(width));
        }

        if (height is <= 0)
        {
            throw new ArgumentException("Height must be greater than zero.", nameof(height));
        }

        AssetId = assetId;
        StorageKey = storageKey.Trim();
        ContentType = contentType.Trim();
        ByteLength = byteLength;
        Width = width;
        Height = height;
        UploadedAt = uploadedAt;
    }

    public Guid AssetId { get; }
    public string StorageKey { get; }
    public string ContentType { get; }
    public long ByteLength { get; }
    public int? Width { get; }
    public int? Height { get; }
    public DateTimeOffset UploadedAt { get; }
}
