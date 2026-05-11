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
    public const string PostDeleted = "POST_DELETED";
}

public sealed record PostSummaryResponse(Guid Id, string AuthorHandle, string Content, Guid? ParentPostId, Guid? OriginalPostId, int LikeCount, bool LikedByCurrentReader);
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
