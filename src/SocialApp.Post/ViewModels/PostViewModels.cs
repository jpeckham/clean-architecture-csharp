namespace SocialApp.Post.ViewModels;

public sealed record QuotedPostSummaryViewModel(Guid Id, string AuthorHandle, string Content, DateTimeOffset CreatedAt);
public sealed record PostMediaSummaryViewModel(
    Guid AssetId,
    string Kind,
    string ContentType,
    long ByteLength,
    int? Width,
    int? Height,
    long? DurationMs,
    int SortOrder,
    string? ThumbnailKey,
    string? AltText,
    string? MediaUrl = null);
public sealed record PostSummaryViewModel(
    Guid Id,
    string AuthorHandle,
    string Content,
    Guid? ParentPostId,
    Guid? OriginalPostId,
    DateTimeOffset CreatedAt,
    int LikeCount,
    bool LikedByCurrentReader,
    int RepostCount,
    bool RepostedByCurrentReader,
    QuotedPostSummaryViewModel? QuotedPost,
    IReadOnlyList<PostMediaSummaryViewModel>? Media = null);
public sealed record CreatePostViewModel(bool Succeeded, string Message, Guid? Id, string? AuthorHandle);
public sealed record ScrollPostsViewModel(IReadOnlyList<PostSummaryViewModel> Posts);
public sealed record SearchPostsViewModel(IReadOnlyList<PostSummaryViewModel> Posts);
public sealed record FollowUserPostsViewModel(bool Succeeded, string Message);
public sealed record BlockUserPostsViewModel(bool Succeeded, string Message);
public sealed record AddLikeToPostViewModel(bool Succeeded, string Message);
public sealed record DeleteLikeFromPostViewModel(bool Succeeded, string Message);
public sealed record ReplyToPostViewModel(bool Succeeded, string Message, Guid? Id, Guid? ParentPostId);
public sealed record RepostViewModel(bool Succeeded, string Message, Guid? Id, Guid? OriginalPostId);
public sealed record DeletePostViewModel(bool Succeeded, string Message);
public sealed record BeginPostMediaUploadViewModel(bool Succeeded, string Message, Guid? AssetId, string? UploadUrl);
public sealed record CompletePostMediaUploadViewModel(bool Succeeded, string Message, Guid? AssetId);
