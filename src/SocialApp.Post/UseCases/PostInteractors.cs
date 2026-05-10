using SocialApp.Post.Entities;
using SocialApp.Post.Gateways;
using SocialApp.Post.RequestModels;
using SocialApp.Post.ResponseModels;

namespace SocialApp.Post.UseCases;

public sealed class CreatePostInteractor(IPostGateway posts, ICreatePostOutputBoundary output) : ICreatePostInputBoundary
{
    public void Handle(CreatePostRequest request) => output.Present(new(true, "Post created.", ToSummary(posts.Save(SocialPost.Create(request.AuthorHandle, request.Content)))));
    internal static PostSummaryResponse ToSummary(SocialPost post) => new(post.Id, post.AuthorHandle, post.Content, post.ParentPostId, post.OriginalPostId, post.LikedBy.Count);
}

public sealed class ScrollPostsInteractor(IPostGateway posts, IScrollPostsOutputBoundary output) : IScrollPostsInputBoundary
{
    public void Handle(ScrollPostsRequest request) => output.Present(new(posts.ScrollFor(request.ReaderHandle, request.Limit).Select(CreatePostInteractor.ToSummary).ToArray()));
}

public sealed class SearchPostsInteractor(IPostSearchGateway search, ISearchPostsOutputBoundary output) : ISearchPostsInputBoundary
{
    public void Handle(SearchPostsRequest request) => output.Present(new(search.Search(request.Query).Select(CreatePostInteractor.ToSummary).ToArray()));
}

public sealed class FollowUserPostsInteractor(IPostGateway posts, IFollowUserPostsOutputBoundary output) : IFollowUserPostsInputBoundary
{
    public void Handle(FollowUserPostsRequest request)
    {
        posts.Follow(request.ReaderHandle, request.FollowedHandle);
        output.Present(new(true, "User followed."));
    }
}

public sealed class BlockUserPostsInteractor(IPostGateway posts, IBlockUserPostsOutputBoundary output) : IBlockUserPostsInputBoundary
{
    public void Handle(BlockUserPostsRequest request)
    {
        posts.Block(request.ReaderHandle, request.BlockedHandle);
        output.Present(new(true, "User blocked."));
    }
}

public sealed class AddLikeToPostInteractor(IPostGateway posts, IAddLikeToPostOutputBoundary output) : IAddLikeToPostInputBoundary
{
    public void Handle(AddLikeToPostRequest request)
    {
        var post = posts.FindById(request.PostId);
        if (post is null) { output.Present(new(false, "Post not found.")); return; }
        post.AddLike(request.Handle);
        output.Present(new(true, "Like added."));
    }
}

public sealed class DeleteLikeFromPostInteractor(IPostGateway posts, IDeleteLikeFromPostOutputBoundary output) : IDeleteLikeFromPostInputBoundary
{
    public void Handle(DeleteLikeFromPostRequest request)
    {
        var post = posts.FindById(request.PostId);
        if (post is null) { output.Present(new(false, "Post not found.")); return; }
        post.DeleteLike(request.Handle);
        output.Present(new(true, "Like deleted."));
    }
}

public sealed class ReplyToPostInteractor(IPostGateway posts, IReplyToPostOutputBoundary output) : IReplyToPostInputBoundary
{
    public void Handle(ReplyToPostRequest request)
    {
        if (posts.FindById(request.ParentPostId) is null) { output.Present(new(false, "Parent post not found.", null)); return; }
        var reply = posts.Save(SocialPost.ReplyTo(request.ParentPostId, request.AuthorHandle, request.Content));
        output.Present(new(true, "Reply created.", CreatePostInteractor.ToSummary(reply)));
    }
}

public sealed class RepostInteractor(IPostGateway posts, IRepostOutputBoundary output) : IRepostInputBoundary
{
    public void Handle(RepostRequest request)
    {
        if (posts.FindById(request.OriginalPostId) is null) { output.Present(new(false, "Original post not found.", null)); return; }
        var repost = posts.Save(SocialPost.Repost(request.OriginalPostId, request.AuthorHandle));
        output.Present(new(true, "Repost created.", CreatePostInteractor.ToSummary(repost)));
    }
}

public sealed class DeletePostInteractor(IPostGateway posts, IDeletePostOutputBoundary output) : IDeletePostInputBoundary
{
    public void Handle(DeletePostRequest request)
    {
        var post = posts.FindById(request.PostId);
        if (post is null) { output.Present(new(false, "Post not found.")); return; }
        post.DeleteBy(request.RequesterHandle);
        posts.Save(post);
        output.Present(new(true, "Post deleted."));
    }
}
