namespace SocialApp.Post.ViewModels;

public sealed record PostSummaryViewModel(Guid Id, string AuthorHandle, string Content, Guid? ParentPostId, Guid? OriginalPostId, int LikeCount);
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
