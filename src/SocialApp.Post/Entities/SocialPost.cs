using System.Text.RegularExpressions;

namespace SocialApp.Post.Entities;

public sealed class SocialPost
{
    private static readonly Regex MentionPattern = new(@"@([a-zA-Z0-9_]+)", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
    private static readonly Regex HashtagPattern = new(@"#(\S+)", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private readonly HashSet<string> _likedBy = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _mentions = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _hashtags = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<PostMediaItem> _media = new();

    private SocialPost(
        Guid id,
        string authorHandle,
        string content,
        Guid? parentPostId,
        Guid? originalPostId,
        DateTimeOffset createdAt,
        IEnumerable<PostMediaItem>? media,
        IEnumerable<string>? mentions,
        IEnumerable<string>? hashtags)
    {
        Id = id;
        AuthorHandle = authorHandle;
        Content = content;
        ParentPostId = parentPostId;
        OriginalPostId = originalPostId;
        CreatedAt = createdAt;
        _media.AddRange(media ?? Array.Empty<PostMediaItem>());
        foreach (var handle in mentions ?? Enumerable.Empty<string>())
            _mentions.Add(handle);
        foreach (var tag in hashtags ?? Enumerable.Empty<string>())
            _hashtags.Add(tag);
    }

    public Guid Id { get; }
    public string AuthorHandle { get; }
    public string Content { get; }
    public Guid? ParentPostId { get; }
    public Guid? OriginalPostId { get; }
    public DateTimeOffset CreatedAt { get; }
    public bool IsDeleted { get; private set; }
    public IReadOnlyCollection<string> LikedBy => _likedBy;
    public IReadOnlyCollection<string> Mentions => _mentions;
    public IReadOnlyCollection<string> Hashtags => _hashtags;
    public IReadOnlyList<PostMediaItem> Media => _media;

    public static SocialPost Create(string authorHandle, string content, IEnumerable<string>? mentions = null, IEnumerable<string>? hashtags = null)
    {
        return Create(authorHandle, content, Array.Empty<PostMediaItem>(), mentions, hashtags);
    }

    public static SocialPost Create(string authorHandle, string content, IReadOnlyList<PostMediaItem> media, IEnumerable<string>? mentions = null, IEnumerable<string>? hashtags = null)
    {
        Validate(authorHandle, content, media);
        return new SocialPost(Guid.NewGuid(), NormalizeHandle(authorHandle), content.Trim(), null, null, DateTimeOffset.UtcNow, media, mentions, hashtags);
    }

    public static SocialPost ReplyTo(Guid parentPostId, string authorHandle, string content, IEnumerable<string>? mentions = null, IEnumerable<string>? hashtags = null)
    {
        Validate(authorHandle, content, Array.Empty<PostMediaItem>());
        return new SocialPost(Guid.NewGuid(), NormalizeHandle(authorHandle), content.Trim(), parentPostId, null, DateTimeOffset.UtcNow, null, mentions, hashtags);
    }

    public static SocialPost Repost(Guid originalPostId, string authorHandle, string content = "", IEnumerable<string>? mentions = null, IEnumerable<string>? hashtags = null)
    {
        if (string.IsNullOrWhiteSpace(NormalizeHandle(authorHandle)))
        {
            throw new ArgumentException("Author handle is required.", nameof(authorHandle));
        }

        if (content.Length > 280)
        {
            throw new ArgumentException("Post content must be 280 characters or fewer.", nameof(content));
        }

        return new SocialPost(Guid.NewGuid(), NormalizeHandle(authorHandle), content.Trim(), null, originalPostId, DateTimeOffset.UtcNow, null, mentions, hashtags);
    }

    public static IEnumerable<string> ExtractMentionHandles(string content) =>
        MentionPattern.Matches(content).Select(m => m.Groups[1].Value);

    public static IEnumerable<string> ExtractHashtags(string content) =>
        HashtagPattern.Matches(content).Select(m => m.Groups[1].Value);

    public static SocialPost Rehydrate(
        Guid id,
        string authorHandle,
        string content,
        Guid? parentPostId,
        Guid? originalPostId,
        DateTimeOffset createdAt,
        bool isDeleted,
        IEnumerable<string> likedBy,
        IReadOnlyList<PostMediaItem>? media = null,
        IEnumerable<string>? mentions = null,
        IEnumerable<string>? hashtags = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Post id is required.", nameof(id));
        }

        if (originalPostId is null)
        {
            Validate(authorHandle, content, media ?? Array.Empty<PostMediaItem>());
        }
        else if (string.IsNullOrWhiteSpace(NormalizeHandle(authorHandle)))
        {
            throw new ArgumentException("Author handle is required.", nameof(authorHandle));
        }
        else if (content.Length > 280)
        {
            throw new ArgumentException("Post content must be 280 characters or fewer.", nameof(content));
        }

        var post = new SocialPost(id, NormalizeHandle(authorHandle), content.Trim(), parentPostId, originalPostId, createdAt, media, mentions, hashtags)
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
        var normalizedHandle = NormalizeHandle(handle);
        if (string.IsNullOrWhiteSpace(normalizedHandle))
        {
            throw new ArgumentException("Handle is required.", nameof(handle));
        }

        _likedBy.Add(normalizedHandle);
    }

    public void DeleteLike(string handle) => _likedBy.Remove(NormalizeHandle(handle));

    public void DeleteBy(string requesterHandle)
    {
        if (!string.Equals(AuthorHandle, NormalizeHandle(requesterHandle), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only the author can delete the post.");
        }

        IsDeleted = true;
    }

    private static void Validate(string authorHandle, string content, IReadOnlyList<PostMediaItem> media)
    {
        if (string.IsNullOrWhiteSpace(NormalizeHandle(authorHandle)))
        {
            throw new ArgumentException("Author handle is required.", nameof(authorHandle));
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

    public static string NormalizeHandle(string handle) =>
        handle.Trim().TrimStart('@').Trim();
}
