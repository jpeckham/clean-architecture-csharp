namespace SocialApp.Post.Entities;

public sealed class SocialPost
{
    private readonly HashSet<string> _likedBy = new(StringComparer.OrdinalIgnoreCase);

    private SocialPost(string authorHandle, string content, Guid? parentPostId, Guid? originalPostId)
    {
        Id = Guid.NewGuid();
        AuthorHandle = authorHandle;
        Content = content;
        ParentPostId = parentPostId;
        OriginalPostId = originalPostId;
        CreatedAt = DateTimeOffset.UtcNow;
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
        return new SocialPost(authorHandle.Trim(), content.Trim(), null, null);
    }

    public static SocialPost ReplyTo(Guid parentPostId, string authorHandle, string content)
    {
        Validate(authorHandle, content);
        return new SocialPost(authorHandle.Trim(), content.Trim(), parentPostId, null);
    }

    public static SocialPost Repost(Guid originalPostId, string authorHandle)
    {
        if (string.IsNullOrWhiteSpace(authorHandle) || !authorHandle.StartsWith('@'))
        {
            throw new ArgumentException("Author handle must start with @.", nameof(authorHandle));
        }

        return new SocialPost(authorHandle.Trim(), string.Empty, null, originalPostId);
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
