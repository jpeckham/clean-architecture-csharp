using SocialApp.Post.ResponseModels;
using SocialApp.Post.UseCases;
using SocialApp.Post.ViewModels;

namespace SocialApp.Post.Presenters;

public sealed class CreatePostPresenter : ICreatePostOutputBoundary
{
    public CreatePostViewModel? ViewModel { get; private set; }
    public void Present(CreatePostResponse response) => ViewModel = new(response.Succeeded, PostMessages.For(response.MessageKey), response.Post?.Id, response.Post?.AuthorHandle);
}

public sealed class ScrollPostsPresenter : IScrollPostsOutputBoundary
{
    public ScrollPostsViewModel? ViewModel { get; private set; }
    public void Present(ScrollPostsResponse response) => ViewModel = new(response.Posts.Select(PostPresenterMapping.ToViewModel).ToArray());
}

public sealed class SearchPostsPresenter : ISearchPostsOutputBoundary
{
    public SearchPostsViewModel? ViewModel { get; private set; }
    public void Present(SearchPostsResponse response) => ViewModel = new(response.Posts.Select(PostPresenterMapping.ToViewModel).ToArray());
}

internal static class PostPresenterMapping
{
    public static PostSummaryViewModel ToViewModel(PostSummaryResponse p) => new(
        p.Id,
        p.AuthorHandle,
        p.Content,
        p.ParentPostId,
        p.OriginalPostId,
        p.CreatedAt,
        p.LikeCount,
        p.LikedByCurrentReader,
        p.RepostCount,
        p.RepostedByCurrentReader,
        p.QuotedPost is null ? null : new(p.QuotedPost.Id, p.QuotedPost.AuthorHandle, p.QuotedPost.Content, p.QuotedPost.CreatedAt),
        p.Media?.Select(m => new PostMediaSummaryViewModel(
            m.AssetId,
            m.Kind,
            m.ContentType,
            m.ByteLength,
            m.Width,
            m.Height,
            m.DurationMs,
            m.SortOrder,
            m.ThumbnailKey,
            m.AltText,
            $"/api/post-media/{m.AssetId}")).ToArray() ?? Array.Empty<PostMediaSummaryViewModel>());
}

public sealed class FollowUserPostsPresenter : IFollowUserPostsOutputBoundary
{
    public FollowUserPostsViewModel? ViewModel { get; private set; }
    public void Present(FollowUserPostsResponse response) => ViewModel = new(response.Succeeded, PostMessages.For(response.MessageKey));
}

public sealed class BlockUserPostsPresenter : IBlockUserPostsOutputBoundary
{
    public BlockUserPostsViewModel? ViewModel { get; private set; }
    public void Present(BlockUserPostsResponse response) => ViewModel = new(response.Succeeded, PostMessages.For(response.MessageKey));
}

public sealed class AddLikeToPostPresenter : IAddLikeToPostOutputBoundary
{
    public AddLikeToPostViewModel? ViewModel { get; private set; }
    public void Present(AddLikeToPostResponse response) => ViewModel = new(response.Succeeded, PostMessages.For(response.MessageKey));
}

public sealed class DeleteLikeFromPostPresenter : IDeleteLikeFromPostOutputBoundary
{
    public DeleteLikeFromPostViewModel? ViewModel { get; private set; }
    public void Present(DeleteLikeFromPostResponse response) => ViewModel = new(response.Succeeded, PostMessages.For(response.MessageKey));
}

public sealed class ReplyToPostPresenter : IReplyToPostOutputBoundary
{
    public ReplyToPostViewModel? ViewModel { get; private set; }
    public void Present(ReplyToPostResponse response) => ViewModel = new(response.Succeeded, PostMessages.For(response.MessageKey), response.Post?.Id, response.Post?.ParentPostId);
}

public sealed class RepostPresenter : IRepostOutputBoundary
{
    public RepostViewModel? ViewModel { get; private set; }
    public void Present(RepostResponse response) => ViewModel = new(response.Succeeded, PostMessages.For(response.MessageKey), response.Post?.Id, response.Post?.OriginalPostId);
}

public sealed class DeletePostPresenter : IDeletePostOutputBoundary
{
    public DeletePostViewModel? ViewModel { get; private set; }
    public void Present(DeletePostResponse response) => ViewModel = new(response.Succeeded, PostMessages.For(response.MessageKey));
}

public sealed class BeginPostMediaUploadPresenter : IBeginPostMediaUploadOutputBoundary
{
    public BeginPostMediaUploadViewModel? ViewModel { get; private set; }
    public void Present(BeginPostMediaUploadResponse response) =>
        ViewModel = new(response.Succeeded, PostMessages.For(response.MessageKey), response.Upload?.AssetId, response.Upload?.UploadUrl);
}

public sealed class CompletePostMediaUploadPresenter : ICompletePostMediaUploadOutputBoundary
{
    public CompletePostMediaUploadViewModel? ViewModel { get; private set; }
    public void Present(CompletePostMediaUploadResponse response) =>
        ViewModel = new(response.Succeeded, PostMessages.For(response.MessageKey), response.Media?.AssetId);
}

internal static class PostMessages
{
    private static readonly IReadOnlyDictionary<string, string> Messages = new Dictionary<string, string>
    {
        [PostMessageKeys.PostCreated] = "Post created.",
        [PostMessageKeys.UserFollowed] = "User followed.",
        [PostMessageKeys.UserBlocked] = "User blocked.",
        [PostMessageKeys.PostNotFound] = "Post not found.",
        [PostMessageKeys.LikeAdded] = "Like added.",
        [PostMessageKeys.LikeDeleted] = "Like deleted.",
        [PostMessageKeys.ParentPostNotFound] = "Parent post not found.",
        [PostMessageKeys.ReplyCreated] = "Reply created.",
        [PostMessageKeys.OriginalPostNotFound] = "Original post not found.",
        [PostMessageKeys.RepostCreated] = "Repost created.",
        [PostMessageKeys.SelfRepostRejected] = "Users cannot repost their own posts.",
        [PostMessageKeys.DuplicateRepostRejected] = "Users can repost a post only once.",
        [PostMessageKeys.RepostDeleted] = "Repost deleted.",
        [PostMessageKeys.RepostNotFound] = "Repost not found.",
        [PostMessageKeys.PostDeleted] = "Post deleted.",
        [PostMessageKeys.PostMediaUploadReserved] = "Post media upload reserved.",
        [PostMessageKeys.PostMediaUploadCompleted] = "Post media upload completed.",
        [PostMessageKeys.PostMediaAssetNotFound] = "Post media asset not found."
    };

    public static string For(string key) => Messages.TryGetValue(key, out var message) ? message : key;
}
