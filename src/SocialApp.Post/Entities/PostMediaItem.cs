namespace SocialApp.Post.Entities;

public enum PostMediaKind
{
    Image,
    Video
}

public sealed class PostMediaItem
{
    public PostMediaItem(
        Guid assetId,
        PostMediaKind kind,
        string storageKey,
        string contentType,
        long byteLength,
        int? width,
        int? height,
        long? durationMs,
        int sortOrder,
        string? thumbnailKey,
        string? altText)
    {
        if (assetId == Guid.Empty)
        {
            throw new ArgumentException("Asset id is required.", nameof(assetId));
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key is required.", nameof(storageKey));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (kind == PostMediaKind.Image && !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Image media requires an image content type.", nameof(contentType));
        }

        if (kind == PostMediaKind.Video && !contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Video media requires a video content type.", nameof(contentType));
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

        if (durationMs is <= 0)
        {
            throw new ArgumentException("Duration must be greater than zero.", nameof(durationMs));
        }

        if (sortOrder < 0)
        {
            throw new ArgumentException("Sort order cannot be negative.", nameof(sortOrder));
        }

        AssetId = assetId;
        Kind = kind;
        StorageKey = storageKey.Trim();
        ContentType = contentType.Trim();
        ByteLength = byteLength;
        Width = width;
        Height = height;
        DurationMs = durationMs;
        SortOrder = sortOrder;
        ThumbnailKey = string.IsNullOrWhiteSpace(thumbnailKey) ? null : thumbnailKey.Trim();
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
    }

    public Guid AssetId { get; }
    public PostMediaKind Kind { get; }
    public string StorageKey { get; }
    public string ContentType { get; }
    public long ByteLength { get; }
    public int? Width { get; }
    public int? Height { get; }
    public long? DurationMs { get; }
    public int SortOrder { get; }
    public string? ThumbnailKey { get; }
    public string? AltText { get; }
}
