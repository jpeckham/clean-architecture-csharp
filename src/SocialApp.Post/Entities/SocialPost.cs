namespace SocialApp.Post.Entities;

public sealed class SocialPost
{
    private readonly HashSet<string> _likedBy = new(StringComparer.OrdinalIgnoreCase);

    private SocialPost(Guid id, string authorHandle, string content, Guid? parentPostId, Guid? originalPostId, DateTimeOffset createdAt)
    {
        Id = id;
        AuthorHandle = authorHandle;
        Content = content;
        ParentPostId = parentPostId;
        OriginalPostId = originalPostId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public string AuthorHandle { get; }
    public string Content { get; }
    public Guid? ParentPostId { get; }
    public Guid? OriginalPostId { get; }
    public DateTimeOffset CreatedAt { get; }
    public bool IsDeleted { get; private set; }
    public IReadOnlyCollection<string> LikedBy => _likedBy;

    public static SocialPost Create(string authorHandle, string content)
    {
        Validate(authorHandle, content);
        return new SocialPost(Guid.NewGuid(), authorHandle.Trim(), content.Trim(), null, null, DateTimeOffset.UtcNow);
    }

    public static SocialPost ReplyTo(Guid parentPostId, string authorHandle, string content)
    {
        Validate(authorHandle, content);
        return new SocialPost(Guid.NewGuid(), authorHandle.Trim(), content.Trim(), parentPostId, null, DateTimeOffset.UtcNow);
    }

    public static SocialPost Repost(Guid originalPostId, string authorHandle)
    {
        if (string.IsNullOrWhiteSpace(authorHandle) || !authorHandle.StartsWith('@'))
        {
            throw new ArgumentException("Author handle must start with @.", nameof(authorHandle));
        }

        return new SocialPost(Guid.NewGuid(), authorHandle.Trim(), string.Empty, null, originalPostId, DateTimeOffset.UtcNow);
    }

    public static SocialPost Rehydrate(
        Guid id,
        string authorHandle,
        string content,
        Guid? parentPostId,
        Guid? originalPostId,
        DateTimeOffset createdAt,
        bool isDeleted,
        IEnumerable<string> likedBy)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Post id is required.", nameof(id));
        }

        if (originalPostId is null)
        {
            Validate(authorHandle, content);
        }
        else if (string.IsNullOrWhiteSpace(authorHandle) || !authorHandle.StartsWith('@'))
        {
            throw new ArgumentException("Author handle must start with @.", nameof(authorHandle));
        }

        var post = new SocialPost(id, authorHandle.Trim(), content.Trim(), parentPostId, originalPostId, createdAt)
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

    private static void Validate(string authorHandle, string content)
    {
        if (string.IsNullOrWhiteSpace(authorHandle) || !authorHandle.StartsWith('@'))
        {
            throw new ArgumentException("Author handle must start with @.", nameof(authorHandle));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Post content is required.", nameof(content));
        }

        if (content.Length > 280)
        {
            throw new ArgumentException("Post content must be 280 characters or fewer.", nameof(content));
        }
    }
}
