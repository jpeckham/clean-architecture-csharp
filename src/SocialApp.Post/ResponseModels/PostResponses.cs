namespace SocialApp.Post.ResponseModels;

public sealed record PostSummaryResponse(Guid Id, string AuthorHandle, string Content, Guid? ParentPostId, Guid? OriginalPostId, int LikeCount);
public sealed record CreatePostResponse(bool Succeeded, string Message, PostSummaryResponse? Post);
public sealed record ScrollPostsResponse(IReadOnlyList<PostSummaryResponse> Posts);
public sealed record SearchPostsResponse(IReadOnlyList<PostSummaryResponse> Posts);
public sealed record FollowUserPostsResponse(bool Succeeded, string Message);
public sealed record BlockUserPostsResponse(bool Succeeded, string Message);
public sealed record AddLikeToPostResponse(bool Succeeded, string Message);
public sealed record DeleteLikeFromPostResponse(bool Succeeded, string Message);
public sealed record ReplyToPostResponse(bool Succeeded, string Message, PostSummaryResponse? Post);
public sealed record RepostResponse(bool Succeeded, string Message, PostSummaryResponse? Post);
public sealed record DeletePostResponse(bool Succeeded, string Message);
