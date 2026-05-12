using SocialApp.Post.Entities;

namespace SocialApp.Post.RequestModels;

public sealed record CreatePostRequest(string AuthorHandle, string Content, IReadOnlyList<Guid>? MediaAssetIds = null);
public sealed record ScrollPostsRequest(string ReaderHandle, int Limit);
public sealed record SearchPostsRequest(string Query, string? ReaderHandle = null);
public sealed record FollowUserPostsRequest(string ReaderHandle, string FollowedHandle);
public sealed record BlockUserPostsRequest(string ReaderHandle, string BlockedHandle);
public sealed record AddLikeToPostRequest(Guid PostId, string Handle);
public sealed record DeleteLikeFromPostRequest(Guid PostId, string Handle);
public sealed record ReplyToPostRequest(Guid ParentPostId, string AuthorHandle, string Content);
public sealed record RepostRequest(Guid OriginalPostId, string AuthorHandle, string Content);
public sealed record DeletePostRequest(Guid PostId, string RequesterHandle);
public sealed record BeginPostMediaUploadRequest(
    string OwnerHandle,
    PostMediaKind Kind,
    string ContentType,
    long ByteLength,
    int? Width,
    int? Height,
    long? DurationMs,
    string? AltText);
public sealed record CompletePostMediaUploadRequest(Guid AssetId, string OwnerHandle);
