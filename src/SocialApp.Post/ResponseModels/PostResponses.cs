namespace SocialApp.Post.ResponseModels;

public static class PostMessageKeys
{
    public const string PostCreated = "POST_CREATED";
    public const string UserFollowed = "USER_FOLLOWED";
    public const string UserBlocked = "USER_BLOCKED";
    public const string PostNotFound = "POST_NOT_FOUND";
    public const string LikeAdded = "LIKE_ADDED";
    public const string LikeDeleted = "LIKE_DELETED";
    public const string ParentPostNotFound = "PARENT_POST_NOT_FOUND";
    public const string ReplyCreated = "REPLY_CREATED";
    public const string OriginalPostNotFound = "ORIGINAL_POST_NOT_FOUND";
    public const string RepostCreated = "REPOST_CREATED";
    public const string SelfRepostRejected = "SELF_REPOST_REJECTED";
    public const string DuplicateRepostRejected = "DUPLICATE_REPOST_REJECTED";
    public const string RepostDeleted = "REPOST_DELETED";
    public const string RepostNotFound = "REPOST_NOT_FOUND";
    public const string PostDeleted = "POST_DELETED";
    public const string PostMediaUploadReserved = "POST_MEDIA_UPLOAD_RESERVED";
    public const string PostMediaUploadCompleted = "POST_MEDIA_UPLOAD_COMPLETED";
    public const string PostMediaAssetNotFound = "POST_MEDIA_ASSET_NOT_FOUND";
}

public sealed record QuotedPostSummaryResponse(Guid Id, string AuthorHandle, string Content, DateTimeOffset CreatedAt, IReadOnlyList<PostMediaSummaryResponse>? Media = null);
public sealed record PostMediaSummaryResponse(
    Guid AssetId,
    string Kind,
    string ContentType,
    long ByteLength,
    int? Width,
    int? Height,
    long? DurationMs,
    int SortOrder,
    string? ThumbnailKey,
    string? AltText);
public sealed record PostSummaryResponse(
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
    QuotedPostSummaryResponse? QuotedPost,
    IReadOnlyList<PostMediaSummaryResponse>? Media = null);
public sealed record CreatePostResponse(bool Succeeded, string MessageKey, PostSummaryResponse? Post);
public sealed record ScrollPostsResponse(IReadOnlyList<PostSummaryResponse> Posts);
public sealed record SearchPostsResponse(IReadOnlyList<PostSummaryResponse> Posts);
public sealed record FollowUserPostsResponse(bool Succeeded, string MessageKey);
public sealed record BlockUserPostsResponse(bool Succeeded, string MessageKey);
public sealed record AddLikeToPostResponse(bool Succeeded, string MessageKey);
public sealed record DeleteLikeFromPostResponse(bool Succeeded, string MessageKey);
public sealed record ReplyToPostResponse(bool Succeeded, string MessageKey, PostSummaryResponse? Post);
public sealed record RepostResponse(bool Succeeded, string MessageKey, PostSummaryResponse? Post);
public sealed record DeletePostResponse(bool Succeeded, string MessageKey);
public sealed record BeginPostMediaUploadResponse(bool Succeeded, string MessageKey, PostMediaUploadResponse? Upload);
public sealed record PostMediaUploadResponse(Guid AssetId, string StorageKey, string UploadUrl);
public sealed record CompletePostMediaUploadResponse(bool Succeeded, string MessageKey, PostMediaSummaryResponse? Media);
