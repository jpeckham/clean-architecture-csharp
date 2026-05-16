using SocialApp.Post.Entities;

namespace SocialApp.Post.Gateways;

public interface IPostGateway
{
    SocialPost Save(SocialPost post);
    SocialPost? FindById(Guid id);
    SocialPost? FindActiveRepost(Guid originalPostId, string authorHandle);
    int CountActiveReposts(Guid originalPostId);
    IReadOnlyList<SocialPost> ScrollFor(string readerHandle, int limit);
    IReadOnlyList<SocialPost> RecentByAuthor(string authorHandle, int limit);
    void Follow(string readerHandle, string followedHandle);
    void Block(string readerHandle, string blockedHandle);
}

public interface IPostSearchGateway
{
    IReadOnlyList<SocialPost> Search(string query);
}

public sealed record ReservePostMediaUpload(
    string OwnerHandle,
    PostMediaKind Kind,
    string ContentType,
    long ByteLength,
    int? Width,
    int? Height,
    long? DurationMs,
    string? ThumbnailKey,
    string? AltText);

public sealed record CompleteReservedPostMediaUpload(Guid AssetId, string OwnerHandle);
public sealed record PostMediaUploadReservation(Guid AssetId, string StorageKey, string UploadUrl);
public sealed record StoredPostMedia(string ContentType, Stream Content);

public interface IPostMediaStorageGateway
{
    PostMediaUploadReservation ReserveUpload(ReservePostMediaUpload upload);
    Task StoreUploadAsync(Guid assetId, Stream content, CancellationToken cancellationToken = default);
    PostMediaItem? CompleteUpload(CompleteReservedPostMediaUpload upload);
    PostMediaItem? FindCompletedAsset(Guid assetId, string ownerHandle);
    StoredPostMedia? FindStored(Guid assetId);
}

public sealed class InMemoryPostGateway : IPostGateway
{
    private readonly List<SocialPost> _posts = new();
    private readonly Dictionary<string, HashSet<string>> _follows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _blocks = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SocialPost> AllPosts => _posts;

    public SocialPost Save(SocialPost post)
    {
        var existingIndex = _posts.FindIndex(p => p.Id == post.Id);
        if (existingIndex >= 0)
        {
            _posts[existingIndex] = post;
            return post;
        }

        _posts.Add(post);
        return post;
    }

    public SocialPost? FindById(Guid id) => _posts.SingleOrDefault(p => p.Id == id);

    public SocialPost? FindActiveRepost(Guid originalPostId, string authorHandle) =>
        _posts.SingleOrDefault(p =>
            !p.IsDeleted &&
            p.OriginalPostId == originalPostId &&
            string.Equals(p.AuthorHandle, SocialPost.NormalizeHandle(authorHandle), StringComparison.OrdinalIgnoreCase));

    public int CountActiveReposts(Guid originalPostId) =>
        _posts.Count(p => !p.IsDeleted && p.OriginalPostId == originalPostId);

    public IReadOnlyList<SocialPost> ScrollFor(string readerHandle, int limit)
    {
        var follows = _follows.GetValueOrDefault(SocialPost.NormalizeHandle(readerHandle)) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var blocks = _blocks.GetValueOrDefault(SocialPost.NormalizeHandle(readerHandle)) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return _posts.Where(p => !p.IsDeleted)
            .Where(p => follows.Count == 0 || follows.Contains(p.AuthorHandle))
            .Where(p => !blocks.Contains(p.AuthorHandle))
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToArray();
    }

    public IReadOnlyList<SocialPost> RecentByAuthor(string authorHandle, int limit) =>
        _posts.Where(p => !p.IsDeleted)
            .Where(p => string.Equals(p.AuthorHandle, SocialPost.NormalizeHandle(authorHandle), StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.CreatedAt)
            .Take(limit)
            .ToArray();

    public void Follow(string readerHandle, string followedHandle) =>
        SetFor(_follows, SocialPost.NormalizeHandle(readerHandle)).Add(SocialPost.NormalizeHandle(followedHandle));

    public void Block(string readerHandle, string blockedHandle) =>
        SetFor(_blocks, SocialPost.NormalizeHandle(readerHandle)).Add(SocialPost.NormalizeHandle(blockedHandle));

    private static HashSet<string> SetFor(Dictionary<string, HashSet<string>> source, string key)
    {
        if (!source.TryGetValue(key, out var values))
        {
            values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            source[key] = values;
        }

        return values;
    }
}

public sealed class InMemoryPostSearchGateway(InMemoryPostGateway posts) : IPostSearchGateway
{
    public IReadOnlyList<SocialPost> Search(string query)
    {
        var allPosts = posts.AllPosts;
        var postsById = allPosts.ToDictionary(p => p.Id);

        return allPosts
            .Where(p => !p.IsDeleted)
            .Where(p => MatchesContent(p, query, postsById))
            .ToArray();
    }

    private static bool MatchesContent(SocialPost post, string query, IReadOnlyDictionary<Guid, SocialPost> postsById) =>
        post.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        post.OriginalPostId is { } originalPostId &&
        postsById.TryGetValue(originalPostId, out var originalPost) &&
        !originalPost.IsDeleted &&
        originalPost.Content.Contains(query, StringComparison.OrdinalIgnoreCase);
}

public sealed class InMemoryPostMediaStorageGateway : IPostMediaStorageGateway
{
    private sealed record ReservedPostMediaUpload(ReservePostMediaUpload Upload, string StorageKey);
    private sealed record CompletedPostMediaUpload(string OwnerHandle, PostMediaItem Item);

    private readonly Dictionary<Guid, ReservedPostMediaUpload> _pending = new();
    private readonly Dictionary<Guid, CompletedPostMediaUpload> _completed = new();

    public PostMediaUploadReservation ReserveUpload(ReservePostMediaUpload upload)
    {
        if (string.IsNullOrWhiteSpace(upload.OwnerHandle))
        {
            throw new ArgumentException("Owner handle is required.", nameof(upload));
        }

        var assetId = Guid.NewGuid();
        var ownerHandle = SocialPost.NormalizeHandle(upload.OwnerHandle);
        var storageKey = $"post-media/{ownerHandle}/{assetId}";
        _pending[assetId] = new ReservedPostMediaUpload(upload with
        {
            OwnerHandle = ownerHandle,
            ContentType = upload.ContentType.Trim()
        }, storageKey);

        return new(assetId, storageKey, $"post-media://{assetId}");
    }

    public Task StoreUploadAsync(Guid assetId, Stream content, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public PostMediaItem? CompleteUpload(CompleteReservedPostMediaUpload upload)
    {
        if (!_pending.TryGetValue(upload.AssetId, out var pending) ||
            !string.Equals(pending.Upload.OwnerHandle, SocialPost.NormalizeHandle(upload.OwnerHandle), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var item = new PostMediaItem(
            upload.AssetId,
            pending.Upload.Kind,
            pending.StorageKey,
            pending.Upload.ContentType,
            pending.Upload.ByteLength,
            pending.Upload.Width,
            pending.Upload.Height,
            pending.Upload.DurationMs,
            0,
            pending.Upload.ThumbnailKey,
            pending.Upload.AltText);

        _pending.Remove(upload.AssetId);
        _completed[upload.AssetId] = new(pending.Upload.OwnerHandle, item);
        return item;
    }

    public PostMediaItem? FindCompletedAsset(Guid assetId, string ownerHandle)
    {
        return _completed.TryGetValue(assetId, out var completed) &&
               string.Equals(completed.OwnerHandle, SocialPost.NormalizeHandle(ownerHandle), StringComparison.OrdinalIgnoreCase)
            ? completed.Item
            : null;
    }

    public StoredPostMedia? FindStored(Guid assetId) =>
        _completed.TryGetValue(assetId, out var completed)
            ? new(completed.Item.ContentType, Stream.Null)
            : null;
}
