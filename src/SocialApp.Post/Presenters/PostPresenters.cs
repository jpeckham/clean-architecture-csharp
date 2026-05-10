using SocialApp.Post.ResponseModels;
using SocialApp.Post.UseCases;
using SocialApp.Post.ViewModels;

namespace SocialApp.Post.Presenters;

public sealed class CreatePostPresenter : ICreatePostOutputBoundary
{
    public CreatePostViewModel? ViewModel { get; private set; }
    public void Present(CreatePostResponse response) => ViewModel = new(response.Succeeded, response.Message, response.Post?.Id, response.Post?.AuthorHandle);
}

public sealed class ScrollPostsPresenter : IScrollPostsOutputBoundary
{
    public ScrollPostsViewModel? ViewModel { get; private set; }
    public void Present(ScrollPostsResponse response) => ViewModel = new(response.Posts.Select(ToViewModel).ToArray());
    private static PostSummaryViewModel ToViewModel(PostSummaryResponse p) => new(p.Id, p.AuthorHandle, p.Content, p.ParentPostId, p.OriginalPostId, p.LikeCount, p.LikedByCurrentReader);
}

public sealed class SearchPostsPresenter : ISearchPostsOutputBoundary
{
    public SearchPostsViewModel? ViewModel { get; private set; }
    public void Present(SearchPostsResponse response) => ViewModel = new(response.Posts.Select(p => new PostSummaryViewModel(p.Id, p.AuthorHandle, p.Content, p.ParentPostId, p.OriginalPostId, p.LikeCount, p.LikedByCurrentReader)).ToArray());
}

public sealed class FollowUserPostsPresenter : IFollowUserPostsOutputBoundary
{
    public FollowUserPostsViewModel? ViewModel { get; private set; }
    public void Present(FollowUserPostsResponse response) => ViewModel = new(response.Succeeded, response.Message);
}

public sealed class BlockUserPostsPresenter : IBlockUserPostsOutputBoundary
{
    public BlockUserPostsViewModel? ViewModel { get; private set; }
    public void Present(BlockUserPostsResponse response) => ViewModel = new(response.Succeeded, response.Message);
}

public sealed class AddLikeToPostPresenter : IAddLikeToPostOutputBoundary
{
    public AddLikeToPostViewModel? ViewModel { get; private set; }
    public void Present(AddLikeToPostResponse response) => ViewModel = new(response.Succeeded, response.Message);
}

public sealed class DeleteLikeFromPostPresenter : IDeleteLikeFromPostOutputBoundary
{
    public DeleteLikeFromPostViewModel? ViewModel { get; private set; }
    public void Present(DeleteLikeFromPostResponse response) => ViewModel = new(response.Succeeded, response.Message);
}

public sealed class ReplyToPostPresenter : IReplyToPostOutputBoundary
{
    public ReplyToPostViewModel? ViewModel { get; private set; }
    public void Present(ReplyToPostResponse response) => ViewModel = new(response.Succeeded, response.Message, response.Post?.Id, response.Post?.ParentPostId);
}

public sealed class RepostPresenter : IRepostOutputBoundary
{
    public RepostViewModel? ViewModel { get; private set; }
    public void Present(RepostResponse response) => ViewModel = new(response.Succeeded, response.Message, response.Post?.Id, response.Post?.OriginalPostId);
}

public sealed class DeletePostPresenter : IDeletePostOutputBoundary
{
    public DeletePostViewModel? ViewModel { get; private set; }
    public void Present(DeletePostResponse response) => ViewModel = new(response.Succeeded, response.Message);
}
