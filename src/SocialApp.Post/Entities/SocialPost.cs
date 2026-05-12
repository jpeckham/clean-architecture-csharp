namespace SocialApp.Post.Entities;

public sealed class SocialPost
{
    private readonly HashSet<string> _likedBy = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PostMediaItem> _media = new();

    private SocialPost(
        Guid id,
        string authorHandle,
        string content,
        Guid? parentPostId,
        Guid? originalPostId,
        DateTimeOffset createdAt,
        IEnumerable<PostMediaItem>? media)
    {
        Id = id;
        AuthorHandle = authorHandle;
        Content = content;
        ParentPostId = parentPostId;
        OriginalPostId = originalPostId;
        CreatedAt = createdAt;
        _media.AddRange(media ?? Array.Empty<PostMediaItem>());
    }

    public Guid Id { get; }
    public string AuthorHandle { get; }
    public string Content { get; }
    public Guid? ParentPostId { get; }
    public Guid? OriginalPostId { get; }
    public DateTimeOffset CreatedAt { get; }
    public bool IsDeleted { get; private set; }
    public IReadOnlyCollection<string> LikedBy => _likedBy;
    public IReadOnlyList<PostMediaItem> Media => _media;

    public static SocialPost Create(string authorHandle, string content)
    {
        return Create(authorHandle, content, Array.Empty<PostMediaItem>());
    }

    public static SocialPost Create(string authorHandle, string content, IReadOnlyList<PostMediaItem> media)
    {
        Validate(authorHandle, content, media);
        return new SocialPost(Guid.NewGuid(), authorHandle.Trim(), content.Trim(), null, null, DateTimeOffset.UtcNow, media);
    }

    public static SocialPost ReplyTo(Guid parentPostId, string authorHandle, string content)
    {
        Validate(authorHandle, content, Array.Empty<PostMediaItem>());
        return new SocialPost(Guid.NewGuid(), authorHandle.Trim(), content.Trim(), parentPostId, null, DateTimeOffset.UtcNow, null);
    }

    public static SocialPost Repost(Guid originalPostId, string authorHandle, string content = "")
    {
        if (string.IsNullOrWhiteSpace(authorHandle) || !authorHandle.StartsWith('@'))
        {
            throw new ArgumentException("Author handle must start with @.", nameof(authorHandle));
        }

        if (content.Length > 280)
        {
            throw new ArgumentException("Post content must be 280 characters or fewer.", nameof(content));
        }

        return new SocialPost(Guid.NewGuid(), authorHandle.Trim(), content.Trim(), null, originalPostId, DateTimeOffset.UtcNow, null);
    }

    public static SocialPost Rehydrate(
        Guid id,
        string authorHandle,
        string content,
        Guid? parentPostId,
        Guid? originalPostId,
        DateTimeOffset createdAt,
        bool isDeleted,
        IEnumerable<string> likedBy,
        IReadOnlyList<PostMediaItem>? media = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Post id is required.", nameof(id));
        }

        if (originalPostId is null)
        {
            Validate(authorHandle, content, media ?? Array.Empty<PostMediaItem>());
        }
        else if (string.IsNullOrWhiteSpace(authorHandle) || !authorHandle.StartsWith('@'))
        {
            throw new ArgumentException("Author handle must start with @.", nameof(authorHandle));
        }
        else if (content.Length > 280)
        {
            throw new ArgumentException("Post content must be 280 characters or fewer.", nameof(content));
        }

        var post = new SocialPost(id, authorHandle.Trim(), content.Trim(), parentPostId, originalPostId, createdAt, media)
        {
            IsDeleted = isDeleted
        };

        foreach (var handle in likedBy)
        {
            post.AddLike(handle);
        }

        return post;
    }

    public void AddLike(string handle)
    {
        if (string.IsNullOrWhiteSpace(handle) || !handle.StartsWith('@'))
        {
            throw new ArgumentException("Handle must start with @.", nameof(handle));
        }

        _likedBy.Add(handle);
    }

    public void DeleteLike(string handle) => _likedBy.Remove(handle);

    public void DeleteBy(string requesterHandle)
    {
        if (!string.Equals(AuthorHandle, requesterHandle, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only the author can delete the post.");
        }

        IsDeleted = true;
    }

    private static void Validate(string authorHandle, string content, IReadOnlyList<PostMediaItem> media)
    {
        if (string.IsNullOrWhiteSpace(authorHandle) || !authorHandle.StartsWith('@'))
        {
            throw new ArgumentException("Author handle must start with @.", nameof(authorHandle));
        }

        if (string.IsNullOrWhiteSpace(content) && media.Count == 0)
        {
            throw new ArgumentException("Post content is required.", nameof(content));
        }

        if (content.Length > 280)
        {
            throw new ArgumentException("Post content must be 280 characters or fewer.", nameof(content));
        }

        var imageCount = media.Count(item => item.Kind == PostMediaKind.Image);
        var videoCount = media.Count(item => item.Kind == PostMediaKind.Video);

        if (imageCount > 4)
        {
            throw new ArgumentException("A post can contain at most 4 images.", nameof(media));
        }

        if (videoCount > 1)
        {
            throw new ArgumentException("A post can contain at most 1 video.", nameof(media));
        }

        if (imageCount > 0 && videoCount > 0)
        {
            throw new ArgumentException("Initial policy does not allow mixing video and images.", nameof(media));
        }
    }
}
