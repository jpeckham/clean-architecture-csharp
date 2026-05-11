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
    public void Present(ScrollPostsResponse response) => ViewModel = new(response.Posts.Select(ToViewModel).ToArray());
    private static PostSummaryViewModel ToViewModel(PostSummaryResponse p) => new(
        p.Id,
        p.AuthorHandle,
        p.Content,
        p.ParentPostId,
        p.OriginalPostId,
        p.LikeCount,
        p.LikedByCurrentReader,
        p.RepostCount,
        p.RepostedByCurrentReader,
        p.QuotedPost is null ? null : new(p.QuotedPost.Id, p.QuotedPost.AuthorHandle, p.QuotedPost.Content));
}

public sealed class SearchPostsPresenter : ISearchPostsOutputBoundary
{
    public SearchPostsViewModel? ViewModel { get; private set; }
    public void Present(SearchPostsResponse response) => ViewModel = new(response.Posts.Select(p => new PostSummaryViewModel(
        p.Id,
        p.AuthorHandle,
        p.Content,
        p.ParentPostId,
        p.OriginalPostId,
        p.LikeCount,
        p.LikedByCurrentReader,
        p.RepostCount,
        p.RepostedByCurrentReader,
        p.QuotedPost is null ? null : new(p.QuotedPost.Id, p.QuotedPost.AuthorHandle, p.QuotedPost.Content))).ToArray());
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
        [PostMessageKeys.PostDeleted] = "Post deleted."
    };

    public static string For(string key) => Messages.TryGetValue(key, out var message) ? message : key;
}
